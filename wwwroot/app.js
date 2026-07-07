//DOLOR
import { createClient } from 'https://cdn.jsdelivr.net/npm/@supabase/supabase-js/+esm'

let connection = null

const supabase = createClient(
  process.env.SUPABASE_URL,
  process.env.SUPABASE_KEY
)

//Evita crash si body está vacío
async function safeJson(response) {
  const text = await response.text()
  if (!text || text.trim() === '') return null
  try { return JSON.parse(text) } catch { return null }
}

let token = null
let currentUserId = null

//Mostrar registro
window.toggleRegister = function () {
  document.getElementById("loginContainer").style.display = "none"
  document.getElementById("registerContainer").style.display = "block"
  document.getElementById("dashboardContainer").style.display = "none"
}

//Mostrar login
window.toggleLogin = function () {
  document.getElementById("registerContainer").style.display = "none"
  document.getElementById("loginContainer").style.display = "block"
  document.getElementById("dashboardContainer").style.display = "none"
}

//Registro
window.register = async function () {
  const email = document.getElementById("regEmail").value
  const password = document.getElementById("regPassword").value

  const { data, error } = await supabase.auth.signUp({ email, password })
  console.log("Respuesta registro:", data, error)

  if (error) {
    alert("Error al registrarse: " + JSON.stringify(error))
  } else {
    alert("Registro exitoso, ahora inicia sesión")
    toggleLogin()
  }
}

//Login
window.login = async function () {
  const email = document.getElementById("email").value
  const password = document.getElementById("password").value

  const { data, error } = await supabase.auth.signInWithPassword({ email, password })
  console.log("Respuesta login:", data, error)

  if (error) {
    alert("Error al iniciar sesión: " + JSON.stringify(error))
    return
  }

  if (data && data.session && data.session.user) {
    token = data.session.access_token
    currentUserId = data.session.user.id
    alert("Login exitoso")

    const loginDiv = document.getElementById("loginContainer")
    const registerDiv = document.getElementById("registerContainer")
    const dashboardDiv = document.getElementById("dashboardContainer")

    if (loginDiv) loginDiv.style.display = "none"
    if (registerDiv) registerDiv.style.display = "none"
    if (dashboardDiv) {
      dashboardDiv.style.display = "flex"
      showSection('inbox')
      await startSignalR()
    }
  } else {
    console.error("No se obtuvo sesión:", data)
  }
}

//Logout
window.logout = async function () {
  await supabase.auth.signOut()
  token = null
  currentUserId = null
  document.getElementById("dashboardContainer").style.display = "none"
  document.getElementById("loginContainer").style.display = "block"
}

//Mostrar secciones
window.showSection = function (section) {
  const sections = ["inbox", "sent", "contacts", "compose", "library"]
  sections.forEach(s => {
    const el = document.getElementById(s + "Section")
    if (el) el.style.display = (s === section) ? "block" : "none"
  })

  if (section === "inbox") loadInbox()
  if (section === "sent") loadSent()
  if (section === "contacts") loadContacts()
  if (section === "library") loadLibrary()
}

// Helper para renderizar archivos
function renderFiles(files) {
  if (!files || files.length === 0) return '';
  let html = '<div style="margin-top:10px; display:flex; flex-direction:column; gap:8px;">';
  files.forEach(f => {
    if (f.file_type === 'image') {
      html += `<img src="${f.file_url}" alt="Attachment" style="max-width:100%; max-height:200px; border-radius:8px;">`;
    } else if (f.file_type === 'video') {
      html += `<video src="${f.file_url}" controls style="max-width:100%; max-height:200px; border-radius:8px;"></video>`;
    } else if (f.file_type === 'audio') {
      html += `<audio src="${f.file_url}" controls style="width:100%;"></audio>`;
    } else {
      html += `<a href="${f.file_url}" target="_blank" style="display:inline-block; padding:8px 12px; background:rgba(255,179,0,0.1); border-radius:6px; border:1px solid rgba(255,179,0,0.3); color:#ffb300; text-decoration:none;">📎 Ver documento</a>`;
    }
  });
  html += '</div>';
  return html;
}

//Inbox
async function loadInbox() {
  //Obtener usuarios para mapear UUID -> Email
  const { data: users } = await supabase.from("users").select("id, email")
  const userMap = {}
  if (users) users.forEach(u => userMap[u.id] = u.email)

  //Mensajes directos al usuario actual
  const { data: direct, error: e1 } = await supabase
    .from("messages")
    .select("id, sender_id, content, created_at, is_massive, files(file_url, file_type)")
    .eq("receiver_id", currentUserId)
    .eq("is_massive", false)
    .order("created_at", { ascending: false })

  //Mensajes masivos (broadcast)
  const { data: broadcasts, error: e2 } = await supabase
    .from("messages")
    .select("id, sender_id, content, created_at, is_massive, files(file_url, file_type)")
    .eq("is_massive", true)
    .order("created_at", { ascending: false })

  const inboxDiv = document.getElementById("inboxMessages")

  if (e1 && e2) {
    inboxDiv.innerHTML = `<p style="color:red;">Error cargando inbox: ${e1?.message}</p>`
    return
  }

  const all = [
    ...(direct || []).map(m => ({ ...m, isBroadcast: false })),
    ...(broadcasts || []).map(m => ({ ...m, isBroadcast: true }))
  ].sort((a, b) => new Date(b.created_at) - new Date(a.created_at))

  if (all.length === 0) {
    inboxDiv.innerHTML = "<p>No hay mensajes en tu inbox.</p>"
    return
  }

  inboxDiv.innerHTML = all.map(m => `
    <div class="msg-card">
      <span class="msg-badge">${m.isBroadcast ? 'Broadcast' : 'Directo'}</span>
      <p><b>De:</b> ${userMap[m.sender_id] || m.sender_id}</p>
      <p>${m.content}</p>
      ${renderFiles(m.files)}
      <small>${new Date(m.created_at).toLocaleString()}</small>
    </div>`).join("")
}

//Enviados
async function loadSent() {
  //Obtener usuarios para mapear UUID -> Email
  const { data: users } = await supabase.from("users").select("id, email")
  const userMap = {}
  if (users) users.forEach(u => userMap[u.id] = u.email)

  const { data, error } = await supabase
    .from("messages")
    .select("id, receiver_id, content, created_at, is_massive, files(file_url, file_type)")
    .eq("sender_id", currentUserId)
    .order("created_at", { ascending: false })

  const sentDiv = document.getElementById("sentMessages")
  if (error) {
    sentDiv.innerHTML = "<p>Error cargando enviados</p>"
  } else {
    if (data.length === 0) {
      sentDiv.innerHTML = "<p>No has enviado ningún mensaje.</p>"
      return
    }
    sentDiv.innerHTML = data.map(m => {
      const typeLabel = m.is_massive ? 'Broadcast' : 'Directo'
      const receiverEmail = m.is_massive ? 'Todos (Broadcast)' : (userMap[m.receiver_id] || m.receiver_id)

      return `
      <div class="msg-card">
        <span class="msg-badge">${typeLabel}</span>
        <p><b>Para:</b> ${receiverEmail}</p>
        <p>${m.content}</p>
        ${renderFiles(m.files)}
        <small>${new Date(m.created_at).toLocaleString()}</small>
      </div>`
    }).join("")
  }
}



//Contactos
async function loadContacts() {
  const { data, error } = await supabase
    .from("users")
    .select("email")

  console.log("Contactos cargados:", data, error)

  const contactsDiv = document.getElementById("contactsList")
  if (error) {
    contactsDiv.innerHTML = "<p>Error cargando contactos</p>"
  } else {
    contactsDiv.innerHTML = data.map(u => `<p>${u.email}  </p>`).join("")
  }
}


//Biblioteca
async function loadLibrary() {
  const { data, error } = await supabase
    .from("files")
    .select("file_url, file_type, created_at")
    .eq("owner_id", currentUserId)
    .order("created_at", { ascending: false })

  const libraryDiv = document.getElementById("libraryFiles")
  if (error) {
    libraryDiv.innerHTML = "<p>Error cargando biblioteca</p>"
  } else {
    libraryDiv.innerHTML = data.map(f => `<p>${f.file_type}: <a href="${f.file_url}" target="_blank">${f.file_url}</a> <small>${f.created_at}</small></p>`).join("")
  }
}

//Cargar contactos en el select de Redactar
async function loadContactsForCompose() {
  const { data, error } = await supabase
    .from("users")
    .select("id, email")


  const select = document.getElementById("receiverSelect")
  if (!error && data) {
    select.innerHTML = '<option value="">Selecciona destinatario...</option>'
    data.forEach(u => {
      select.innerHTML += `<option value="${u.id}">${u.email}</option>`
    })
  }
}


//Alternar formularios de Redactar
window.showComposeForm = function (mode) {
  document.getElementById("directForm").style.display = (mode === "direct") ? "block" : "none"
  document.getElementById("broadcastForm").style.display = (mode === "broadcast") ? "block" : "none"

  if (mode === "direct") loadContactsForCompose()
}

//Subir archivos a Supabase Storage
async function uploadFiles(filesInput) {
  const urls = []
  for (const file of Array.from(filesInput)) {
    const path = `uploads/${currentUserId}/${Date.now()}_${file.name}`
    const { error } = await supabase.storage
      .from("hermess-files")
      .upload(path, file, { upsert: true })

    if (error) {
      console.error("Error subiendo archivo:", error)
    } else {
      const { data: urlData } = supabase.storage
        .from("hermess-files")
        .getPublicUrl(path)
      urls.push(urlData.publicUrl)
    }
  }
  return urls
}

//Preparar envío
window.prepareSend = async function (mode) {
  if (mode === "direct") {
    const receiverId = document.getElementById("receiverSelect").value
    const content = document.getElementById("directContent").value
    const filesInput = document.getElementById("directFileInput").files

    if (!receiverId) { alert("Selecciona un destinatario"); return }
    if (!content.trim()) { alert("Escribe un mensaje"); return }

    let fileUrls = []
    if (filesInput && filesInput.length > 0) fileUrls = await uploadFiles(filesInput)

    await sendMessage(receiverId, content, fileUrls)
  } else {
    const content = document.getElementById("broadcastContent").value
    const filesInput = document.getElementById("broadcastFileInput").files

    if (!content.trim()) { alert("Escribe un mensaje"); return }

    let fileUrls = []
    if (filesInput && filesInput.length > 0) fileUrls = await uploadFiles(filesInput)

    await broadcastMessage(content, fileUrls)
  }
}


//Enviar mensaje directo
async function sendMessage(receiverId, content, files = []) {
  try {
    const response = await fetch("/api/messages/send", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        "Authorization": "Bearer " + token
      },
      body: JSON.stringify({ receiverId, content, files })
    })

    const result = await safeJson(response)
    console.log("Respuesta envío directo:", response.status, result)

    if (!response.ok) {
      const errMsg = result?.error || result?.message || `Error HTTP ${response.status}`
      alert("Error al enviar: " + errMsg)
      return
    }

    alert("Mensaje directo enviado")
    document.getElementById("directContent").value = ""
    document.getElementById("directFileInput").value = ""
  } catch (err) {
    console.error("Error en sendMessage:", err)
    alert("Error de red: " + err.message)
  }
}

//Broadcast
async function broadcastMessage(content, files = []) {
  try {
    const response = await fetch("/api/messages/broadcast", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        "Authorization": "Bearer " + token
      },
      body: JSON.stringify({ content, files })
    })

    const result = await safeJson(response)
    console.log("Respuesta broadcast:", response.status, result)

    if (!response.ok) {
      const errMsg = result?.error || result?.message || `Error HTTP ${response.status}`
      alert("Error al enviar broadcast: " + errMsg)
      return
    }

    alert("Mensaje masivo enviado")
    document.getElementById("broadcastContent").value = ""
    document.getElementById("broadcastFileInput").value = ""
  } catch (err) {
    console.error("Error en broadcastMessage:", err)
    alert("Error de red: " + err.message)
  }
}

async function startSignalR() {
  connection = new signalR.HubConnectionBuilder()
    .withUrl("/chatHub", {
      accessTokenFactory: () => token
    })
    .withAutomaticReconnect()
    .build()

  //listeners...
  connection.on("ReceiveMessage", (message) => {
    console.log("Mensaje directo recibido:", message)
    const banner = document.getElementById("notifBanner")
    if (banner) {
      banner.textContent = `Nuevo mensaje de ${message.senderId}: ${message.content}`
      banner.style.display = "block"
      setTimeout(() => banner.style.display = "none", 5000)
    }
    loadInbox()
  })

  connection.on("ReceiveBroadcast", (message) => {
    console.log("Broadcast recibido:", message)
    const banner = document.getElementById("notifBanner")
    if (banner) {
      banner.textContent = `Broadcast: ${message.content}`
      banner.style.display = "block"
      setTimeout(() => banner.style.display = "none", 5000)
    }
    loadInbox()
  })

  try {
    await connection.start()
    console.log("Conectado a SignalR ChatHub")
  } catch (err) {
    console.error("Error conectando a SignalR:", err)
  }
}


