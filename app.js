"use strict";

let products = [
  {id:1,name:"Picanha na brasa",category:"Cortes",description:"Picanha fatiada, farofa da casa e vinagrete fresco.",price:49.9,image:"https://images.unsplash.com/photo-1755437103545-6aca71afa6b7?auto=format&fit=crop&w=900&q=85"},
  {id:2,name:"Costela fogo de chão",category:"Cortes",description:"Costela assada lentamente, macia e defumada.",price:54.9,image:"https://images.unsplash.com/photo-1679711246825-1f2bd51b16d0?auto=format&fit=crop&w=900&q=85"},
  {id:3,name:"Combo Churrasco",category:"Combos",description:"Picanha, linguiça, pão de alho e acompanhamentos.",price:89.9,image:"https://images.unsplash.com/photo-1611354609291-69aba96e45df?auto=format&fit=crop&w=900&q=85"},
  {id:4,name:"Espetinho misto",category:"Espetinhos",description:"Alcatra, linguiça artesanal e cebola tostada.",price:24.9,image:"https://images.unsplash.com/photo-1611354574034-9655b5b72d59?auto=format&fit=crop&w=900&q=85"},
  {id:5,name:"Linguiça artesanal",category:"Cortes",description:"Linguiça suculenta na brasa com molho de pimenta.",price:26.9,image:"https://images.unsplash.com/photo-1611354609291-69aba96e45df?auto=format&fit=crop&w=900&q=85"},
  {id:6,name:"Pão de alho tostado",category:"Acompanhamentos",description:"Pão crocante com manteiga de alho e ervas.",price:14.9,image:"https://images.unsplash.com/photo-1611354574034-9655b5b72d59?auto=format&fit=crop&w=900&q=85"},
  {id:7,name:"Queijo coalho na brasa",category:"Acompanhamentos",description:"Queijo dourado, melado de cana e orégano.",price:19.9,image:"https://images.unsplash.com/photo-1755437103545-6aca71afa6b7?auto=format&fit=crop&w=900&q=85"},
  {id:8,name:"Churrasco da família",category:"Combos",description:"Seleção de carnes e acompanhamentos para compartilhar.",price:139.9,image:"https://images.unsplash.com/photo-1679711246825-1f2bd51b16d0?auto=format&fit=crop&w=900&q=85"}
];
const read = (key, fallback) => { try { return JSON.parse(localStorage.getItem(`brasa-${key}`)) ?? fallback; } catch { return fallback; } };
const save = (key, value) => localStorage.setItem(`brasa-${key}`, JSON.stringify(value));
let cart = read("cart", []), user = read("user", null), orders = read("orders", []), selectedCategory = "Todos", toastTimer;
const apiBase = "http://localhost:5000";
let apiOnline = false;
const money = n => n.toLocaleString("pt-BR", {style:"currency", currency:"BRL"});
const $ = s => document.querySelector(s);

function renderProducts(){
  const query = $("#searchInput").value.trim().toLocaleLowerCase("pt-BR");
  const list = products.filter(p => (selectedCategory === "Todos" || p.category === selectedCategory) && `${p.name} ${p.description} ${p.category}`.toLocaleLowerCase("pt-BR").includes(query));
  $("#productGrid").innerHTML = list.map(p => `<article class="product-card"><img class="product-image" src="${p.image}" alt="${p.name}" loading="lazy"><div class="product-details"><span class="product-category">${p.category}</span><h3>${p.name}</h3><p class="product-description">${p.description}</p><div class="product-bottom"><span class="product-price">${money(p.price)}</span><button class="add-button" data-add="${p.id}" aria-label="Adicionar ${p.name} à sacola">+</button></div></div></article>`).join("");
  $("#emptyState").hidden = list.length > 0;
}
function renderCart(){
  const count = cart.reduce((sum,item)=>sum+item.quantity,0), total = cart.reduce((sum,item)=>sum+products.find(p=>p.id===item.id).price*item.quantity,0);
  $("#cartCount").textContent = count; $("#cartTotal").textContent = money(total);
  $("#cartItems").innerHTML = cart.length ? cart.map(item => {const p=products.find(x=>x.id===item.id);return `<div class="cart-item"><div><b>${p.name}</b><small>${money(p.price)} cada</small></div><div class="quantity-controls"><button data-quantity="${p.id}" data-delta="-1" aria-label="Remover uma unidade de ${p.name}">−</button><span>${item.quantity}</span><button data-quantity="${p.id}" data-delta="1" aria-label="Adicionar uma unidade de ${p.name}">+</button></div><b>${money(p.price*item.quantity)}</b></div>`;}).join("") : `<p class="cart-empty">Sua sacola ainda está esperando por algo gostoso.</p>`;
  $("#checkoutButton").disabled = !cart.length; save("cart",cart);
}
function renderUser(){
  $("#profileName").textContent = user ? `Olá, ${user.name.split(" ")[0]}!` : "Olá! Que bom ter você aqui.";
  $("#profileContact").textContent = user ? `${user.email}${user.phone ? ` · ${user.phone}` : ""}` : "Entre na sua conta para ver seus dados e pedidos.";
  $("#avatarInitials").textContent = user ? user.name.split(/\s+/).slice(0,2).map(n=>n[0]).join("").toUpperCase() : "BB";
  $("#profileAction").textContent = user ? "Sair da conta" : "Entrar / Criar conta";
}
function renderOrders(){
  const el = $("#ordersList");
  el.innerHTML = orders.length ? orders.map(o=>`<article class="order-card"><div><strong>Pedido #${String(o.id).slice(-5)}</strong><small>${new Date(o.date).toLocaleDateString("pt-BR")} · ${o.items.map(i=>`${i.quantity}× ${products.find(p=>p.id===i.id).name}`).join(", ")}</small></div><span class="order-status">${o.status}</span><span class="order-total">${money(o.total)}</span></article>`).join("") : `<div class="orders-empty">Seus pedidos vão aparecer aqui. Que tal escolher algo gostoso? 🍽️</div>`;
}
function exportOrdersJson(){
  const payload=orders.map(o=>({siteOrderId:String(o.id),date:o.date,status:o.status,total:Number(o.total),customerEmail:user?.email||"",items:o.items.map(i=>{const p=products.find(x=>x.id===i.id);return {productId:i.id,productName:p?.name||`Produto ${i.id}`,quantity:i.quantity,unitPrice:p?.price||0};})}));
  const blob=new Blob([JSON.stringify(payload,null,2)],{type:"application/json"});const url=URL.createObjectURL(blob);const a=document.createElement("a");a.href=url;a.download="brasa-pedidos.json";a.click();URL.revokeObjectURL(url);showToast(`${payload.length} pedido(s) exportado(s) para o aplicativo desktop.`);
}
async function refreshOrders(){
  if(!apiOnline||!user?.id)return;
  try{const data=await apiRequest(`/api/pedidos?usuarioId=${user.id}`);orders=data.map(o=>({id:o.id,date:o.criadoEm,items:o.itens.map(i=>({id:i.produtoId,quantity:i.quantidade})),total:o.total,status:o.status}));save("orders",orders);renderOrders();}catch(error){showToast(error.message);}
}
async function apiRequest(path,options={}){
  const response=await fetch(`${apiBase}${path}`,{...options,headers:{"Content-Type":"application/json",...(options.headers||{})}});
  if(!response.ok){let message=`Falha na API (${response.status}).`;try{message=(await response.json()).mensagem||message;}catch{}throw new Error(message);}
  if(response.status===204)return null;
  return response.json();
}
function showToast(message){const el=$("#toast");el.textContent=message;el.classList.add("show");clearTimeout(toastTimer);toastTimer=setTimeout(()=>el.classList.remove("show"),2300);}
function openAuth(mode="login"){document.querySelectorAll(".auth-tab").forEach(b=>b.classList.toggle("active",b.dataset.mode===mode));setAuthMode(mode);$("#authMessage").textContent="";$("#authModal").showModal();}
function setAuthMode(mode){const signup=mode==="signup";$("#authTitle").innerHTML=signup?"Vamos preparar sua <em>mesa.</em>":"Que bom ter você <em>por aqui.</em>";$("#nameField").hidden=!signup;$("#phoneLabel").hidden=!signup;$("#nameField").required=signup;$("#phoneField").required=signup;$("#passwordField").autocomplete=signup?"new-password":"current-password";$("#authSubmit").innerHTML=signup?"Criar minha conta <span>→</span>":"Entrar na conta <span>→</span>";}

document.addEventListener("click", event=>{
  const add=event.target.closest("[data-add]"); if(add){const id=Number(add.dataset.add),item=cart.find(x=>x.id===id);item?item.quantity++:cart.push({id,quantity:1});renderCart();showToast("Prato adicionado à sacola!");}
  const qty=event.target.closest("[data-quantity]"); if(qty){const id=Number(qty.dataset.quantity),item=cart.find(x=>x.id===id);item.quantity+=Number(qty.dataset.delta);if(item.quantity<=0)cart=cart.filter(x=>x.id!==id);renderCart();}
});
$("#categoryList").addEventListener("click",e=>{const b=e.target.closest("[data-category]");if(!b)return;selectedCategory=b.dataset.category;document.querySelectorAll(".category").forEach(x=>x.classList.toggle("active",x===b));renderProducts();});
$("#searchInput").addEventListener("input",renderProducts);
$("#cartOpen").addEventListener("click",()=>{$("#cartMessage").textContent="";$("#cartModal").showModal();});
$("#exportOrders").addEventListener("click",exportOrdersJson);
$("#loginOpen").addEventListener("click",()=>openAuth("login"));$("#signupOpen").addEventListener("click",()=>openAuth("signup"));$("#profileAction").addEventListener("click",()=>{if(user){user=null;save("user",user);renderUser();showToast("Você saiu da sua conta.");}else openAuth("login");});
document.querySelectorAll(".auth-tab").forEach(b=>b.addEventListener("click",()=>{document.querySelectorAll(".auth-tab").forEach(x=>x.classList.toggle("active",x===b));setAuthMode(b.dataset.mode);$("#authMessage").textContent="";}));
$("#authForm").addEventListener("submit",async e=>{e.preventDefault();const mode=document.querySelector(".auth-tab.active").dataset.mode,name=$("#nameField").value.trim(),email=$("#emailField").value.trim().toLowerCase(),password=$("#passwordField").value,phone=$("#phoneField").value;if(mode==="signup"&&(name.length<3||phone.replace(/\D/g,"").length<10)){$("#authMessage").textContent="Confira seu nome e informe um celular válido com DDD.";return;}if(password.length<6){$("#authMessage").textContent="A senha precisa ter pelo menos 6 caracteres.";return;}try{if(apiOnline){user=mode==="signup"?await apiRequest("/api/usuarios",{method:"POST",body:JSON.stringify({nome:name,email,telefone:phone,senha:password})}):await apiRequest("/api/sessoes",{method:"POST",body:JSON.stringify({email,senha:password})});user.name=user.nome;user.email=user.email;user.phone=user.telefone;user.id=user.id;}else{user={name:mode==="signup"?name:(user?.email===email?user.name:email.split("@")[0]),email,phone:mode==="signup"?phone:(user?.phone||"")};}}catch(error){$("#authMessage").textContent=error.message;return;}save("user",user);renderUser();$("#authModal").close();showToast(mode==="signup"?"Conta criada! Bem-vindo à mesa.":"Login realizado. Bom ter você aqui!");refreshOrders();});
$("#phoneField").addEventListener("input",e=>{let n=e.target.value.replace(/\D/g,"").slice(0,11);if(n.length>10)e.target.value=n.replace(/^(\d{2})(\d{5})(\d{0,4}).*/,"($1) $2-$3");else if(n.length>6)e.target.value=n.replace(/^(\d{2})(\d{4})(\d{0,4}).*/,"($1) $2-$3");else if(n.length>2)e.target.value=n.replace(/^(\d{2})(\d{0,5})/,"($1) $2");else e.target.value=n;});
$("#checkoutButton").addEventListener("click",async()=>{if(!cart.length)return;if(!user){$("#cartModal").close();openAuth("login");$("#authMessage").textContent="Entre ou crie uma conta para continuar seu pedido.";return;}const total=cart.reduce((s,i)=>s+products.find(p=>p.id===i.id).price*i.quantity,0);if(apiOnline&&user.id){try{await apiRequest("/api/pedidos",{method:"POST",body:JSON.stringify({usuarioId:user.id,itens:cart.map(i=>({produtoId:i.id,quantidade:i.quantity}))})});}catch(error){$("#cartMessage").textContent=error.message;return;}}else{orders.unshift({id:Date.now(),date:new Date().toISOString(),items:cart.map(i=>({...i})),total,status:"Recebido"});save("orders",orders);}cart=[];renderCart();renderOrders();$("#cartModal").close();showToast("Pedido confirmado! Já vamos acender a brasa.");document.querySelector("#pedidos").scrollIntoView({behavior:"smooth"});refreshOrders();});
$("#themeToggle").addEventListener("click",()=>{const isLight=document.body.classList.toggle("light");localStorage.setItem("brasa-theme-v2",isLight?"light":"dark");$("#themeToggle").textContent=isLight?"☾":"☀";});
$("#menuToggle").addEventListener("click",()=>{const open=$("#mainNav").classList.toggle("open");$("#menuToggle").setAttribute("aria-expanded",String(open));});document.querySelectorAll("#mainNav a").forEach(a=>a.addEventListener("click",()=>{$("#mainNav").classList.remove("open");$("#menuToggle").setAttribute("aria-expanded","false");}));
$("#newsletterForm").addEventListener("submit",e=>{e.preventDefault();showToast("Obrigado! Em breve chega um bilhete gostoso.");e.target.reset();});
const isLight=localStorage.getItem("brasa-theme-v2")==="light";document.body.classList.toggle("light",isLight);$("#themeToggle").textContent=isLight?"☾":"☀";renderProducts();renderCart();renderUser();renderOrders();
async function connectApi(){try{const remote=await apiRequest("/api/produtos");if(remote.length){const images=new Map(products.map(p=>[p.id,p.image]));products=remote.map(p=>({id:p.id,name:p.nome,category:p.categoria,description:p.descricao,price:Number(p.preco),image:p.imagemUrl||images.get(p.id)||images.get(1)}));apiOnline=true;cart=cart.filter(i=>products.some(p=>p.id===i.id));renderProducts();renderCart();if(user?.id)refreshOrders();}}catch{/* API is optional while the front-end is being demonstrated locally. */}}
connectApi();
