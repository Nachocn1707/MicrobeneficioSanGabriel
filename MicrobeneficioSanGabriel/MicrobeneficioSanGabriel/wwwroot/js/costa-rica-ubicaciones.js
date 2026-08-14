(() => {
    "use strict";

    // División territorial vigente utilizada para las listas dependientes.
    const ubicaciones = {"San José":{"San José":["Carmen","Merced","Hospital","Catedral","Zapote","San Francisco De Dos Rios","Uruca","Mata Redonda","Pavas","Hatillo","San Sebastian"],"Escazu":["Escazu","San Antonio","San Rafael"],"Desamparados":["Desamparados","San Miguel","San Juan De Dios","San Rafael Arriba","San Antonio","Frailes","Patarra","San Cristóbal","Rosario","Damas","San Rafael Abajo","Gravilias","Los Guido"],"Puriscal":["Santiago","Mercedes Sur","Barbacoas","Grifo Alto","San Rafael","Candelarita","Desamparaditos","San Antonio","Chires"],"Tarrazu":["San Marcos","San Lorenzo","San Carlos"],"Aserrí":["Aserrí","Tarbaca","Vuelta De Jorco","San Gabriel","Legua","Monterrey","Salitrillos"],"Mora":["Colon","Guayabo","Tabarcia","Piedras Negras","Picagres","Jaris","Quitirrisi"],"Goicoechea":["Guadalupe","San Francisco","Calle Blancos","Mata De Platano","Ipis","Rancho Redondo","Purral"],"Santa Ana":["Santa Ana","Salitral","Pozos","Uruca","Piedades","Brasil"],"Alajuelita":["Alajuelita","San Josecito","San Antonio","Concepción","San Felipe"],"Vázquez de Coronado":["San Isidro","San Rafael","Dulce Nombre De Jesus","Patalillo","Cascajal"],"Acosta":["San Ignacio","Guaitil","Palmichal","Cangrejal","Sabanillas"],"Tibás":["San Juan","Cinco Esquinas","Anselmo Llorente","Leon Xiii","Colima"],"Moravia":["San Vicente","San Jerónimo","La Trinidad"],"Montes De Oca":["San Pedro","Sabanilla","Mercedes","San Rafael"],"Turrubares":["San Pablo","San Pedro","San Juan De Mata","San Luis","Carara"],"Dota":["Santa Maria","Jardin","Copey"],"Curridabat":["Curridabat","Granadilla","Sanchez","Tirrases"],"Pérez Zeledón":["San Isidro De El General","El General","Daniel Flores","Rivas","San Pedro","Platanares","Pejibaye","Cajon","Baru","Rio Nuevo","Paramo","La Amistad"],"León Cortés Castro":["San Pablo","San Andres","Llano Bonito","San Isidro","Santa Cruz","San Antonio"]},"Alajuela":{"Alajuela":["Alajuela","San José","Carrizal","San Antonio","Guacima","San Isidro","Sabanilla","San Rafael","Río Segundo","Desamparados","Turrucares","Tambor","Garita","Sarapiqui"],"San Ramón":["San Ramón","Santiago","San Juan","Piedades Norte","Piedades Sur","San Rafael","San Isidro","Angeles","Alfaro","Volio","Concepción","Zapotal","Peñas Blancas","San Lorenzo"],"Grecia":["Grecia","San Isidro","San José","San Roque","Tacares","Puente De Piedra","Bolivar"],"San Mateo":["San Mateo","Desmonte","Jesus Maria","Labrador"],"Atenas":["Atenas","Jesús","Mercedes","San Isidro","Concepción","San José","Santa Eulalia","Escobal"],"Naranjo":["Naranjo","San Miguel","San José","Cirri Sur","San Jerónimo","San Juan","El Rosario","Palmitos"],"Palmares":["Palmares","Zaragoza","Buenos Aires","Santiago","Candelaria","Esquipulas","La Granja"],"Poás":["San Pedro","San Juan","San Rafael","Carrillos","Sabana Redonda"],"Orotina":["Orotina","El Mastate","Hacienda Vieja","Coyolar","La Ceiba"],"San Carlos":["Quesada","Florencia","Buenavista","Aguas Zarcas","Venecia","Pital","La Fortuna","La Tigra","La Palmera","Venado","Cutris","Monterrey","Pocosol"],"Zarcero":["Zarcero","Laguna","Tapesco","Guadalupe","Palmira","Zapote","Brisas"],"Sarchi":["Sarchí Norte","Sarchí Sur","Toro Amarillo","San Pedro","Rodriguez"],"Upala":["Upala","Aguas Claras","San Jose O Pizote","Bijagua","Delicias","Dos Rios","Yolillal","Canalete"],"Los Chiles":["Los Chiles","Caño Negro","El Amparo","San Jorge"],"Guatuso":["San Rafael","Buenavista","Cote","Katira"],"Río Cuarto":["Río Cuarto","Santa Rita","Santa Isabel"]},"Cartago":{"Cartago":["Oriental","Occidental","Carmen","San Nicolás","Aguacaliente O San Francisco","Guadalupe O Arenilla","Corralillo","Tierra Blanca","Dulce Nombre","Llano Grande","Quebradilla"],"Paraíso":["Paraíso","Santiago","Orosi","Cachi","Llanos De Santa Lucia","Birrisito"],"La Union":["Tres Rios","San Diego","San Juan","San Rafael","Concepción","Dulce Nombre","San Ramón","Río Azul"],"Jiménez":["Juan Viñas","Tucurrique","Pejibaye","La Victoria"],"Turrialba":["Turrialba","La Suiza","Peralta","Santa Cruz","Santa Teresita","Pavones","Tuis","Tayutic","Santa Rosa","Tres Equis"],"Alvarado":["Pacayas","Cervantes","Capellades"],"Oreamuno":["San Rafael","Cot","Potrero Cerrado","Cipreses","Santa Rosa"],"El Guarco":["El Tejar","San Isidro","Tobosi","Patio De Agua"]},"Heredia":{"Heredia":["Heredia","Mercedes","San Francisco","Ulloa","Varablanca"],"Barva":["Barva","San Pedro","San Pablo","San Roque","Santa Lucia","San Jose De La Montaña","Puente Salas"],"Santo Domingo":["Santo Domingo","San Vicente","San Miguel","Paracito","Santo Tomas","Santa Rosa","Tures","Para"],"Santa Barbara":["Santa Barbara","San Pedro","San Juan","Jesús","Santo Domingo","Puraba"],"San Rafael":["San Rafael","San Josecito","Santiago","Los Angeles","Concepción"],"San Isidro":["San Isidro","San José","Concepción","San Francisco"],"Belén":["San Antonio","La Ribera","La Asuncion"],"Flores":["San Joaquin","Barrantes","Llorente"],"San Pablo":["San Pablo","Rincon De Sabanilla"],"Sarapiquí":["Puerto Viejo","La Virgen","Las Horquetas","Llanuras Del Gaspar","Cureña"]},"Guanacaste":{"Liberia":["Liberia","Cañas Dulces","Mayorga","Nacascolo","Curubande"],"Nicoya":["Nicoya","Mansion","San Antonio","Quebrada Honda","Samara","Nosara","Belen De Nosarita"],"Santa Cruz":["Santa Cruz","Bolson","Veintisiete De Abril","Tempate","Cartagena","Cuajiniquil","Diria","Cabo Velas","Tamarindo"],"Bagaces":["Bagaces","La Fortuna","Mogote","Rio Naranjo"],"Carrillo":["Filadelfia","Palmira","Sardinal","Belén"],"Cañas":["Cañas","Palmira","San Miguel","Bebedero","Porozal"],"Abangares":["Las Juntas","Sierra","San Juan","Colorado"],"Tilarán":["Tilarán","Quebrada Grande","Tronadora","Santa Rosa","Libano","Tierras Morenas","Arenal","Cabeceras"],"Nandayure":["Carmona","Santa Rita","Zapotal","San Pablo","Porvenir","Bejuco"],"La Cruz":["La Cruz","Santa Cecilia","La Garita","Santa Elena"],"Hojancha":["Hojancha","Monte Romo","Puerto Carrillo","Huacas","Matambu"]},"Puntarenas":{"Puntarenas":["Puntarenas","Pitahaya","Chomes","Lepanto","Paquera","Manzanillo","Guacimal","Barranca","Isla Del Coco","Cobano","Chacarita","Chira","Acapulco","El Roble","Arancibia"],"Esparza":["Espiritu Santo","San Juan Grande","Macacona","San Rafael","San Jerónimo","Caldera"],"Buenos Aires":["Buenos Aires","Volcan","Potrero Grande","Boruca","Pilas","Colinas","Changuena","Biolley","Brunka"],"Montes de Oro":["Miramar","La Union","San Isidro"],"Osa":["Puerto Cortes","Palmar","Sierpe","Bahia Ballena","Piedras Blancas","Bahia Drake"],"Quepos":["Quepos","Savegre","Naranjito"],"Golfito":["Golfito","Guaycara","Pavon"],"Coto Brus":["San Vito","Sabalito","Aguabuena","Limoncito","Pittier","Gutierrez Braun"],"Parrita":["Parrita"],"Corredores":["Corredor","La Cuesta","Canoas","Laurel"],"Garabito":["Jaco","Tarcoles","Lagunillas"],"Monteverde":["Monteverde"],"Puerto Jiménez":["Puerto Jimenez"]},"Limón":{"Limón":["Limón","Valle La Estrella","Rio Blanco","Matama"],"Pococí":["Guapiles","Jiménez","Rita","Roxana","Cariari","Colorado","La Colonia"],"Siquirres":["Siquirres","Pacuarito","Florida","Germania","El Cairo","Alegria","Reventazon"],"Talamanca":["Bratsi","Sixaola","Cahuita","Telire"],"Matina":["Matina","Batan","Carrandi"],"Guácimo":["Guácimo","Mercedes","Pocora","Río Jiménez","Duacari"]}};

    const normalizar = valor => (valor || "")
        .normalize("NFD")
        .replace(/[\u0300-\u036f]/g, "")
        .trim()
        .toLowerCase();

    const buscarClave = (objeto, valor) => {
        const buscado = normalizar(valor);
        return Object.keys(objeto || {}).find(k => normalizar(k) === buscado) || "";
    };

    const agregarOpcion = (select, valor, texto = valor) => {
        const option = document.createElement("option");
        option.value = valor;
        option.textContent = texto;
        select.appendChild(option);
    };

    const cargarProvincias = (provincia, canton, distrito) => {
        const actualProvincia = provincia.dataset.currentValue || provincia.value || "";
        provincia.innerHTML = '<option value="">Seleccione una provincia</option>';
        Object.keys(ubicaciones).forEach(nombre => agregarOpcion(provincia, nombre));

        const clave = buscarClave(ubicaciones, actualProvincia);
        if (clave) provincia.value = clave;

        cargarCantones(provincia, canton, distrito, canton.dataset.currentValue || canton.value || "", distrito.dataset.currentValue || distrito.value || "");
    };

    const cargarCantones = (provincia, canton, distrito, cantonActual = "", distritoActual = "") => {
        canton.innerHTML = '<option value="">Seleccione un cantón</option>';
        distrito.innerHTML = '<option value="">Seleccione un distrito</option>';
        canton.disabled = !provincia.value;
        distrito.disabled = true;

        if (!provincia.value || !ubicaciones[provincia.value]) return;

        Object.keys(ubicaciones[provincia.value]).forEach(nombre => agregarOpcion(canton, nombre));
        const claveCanton = buscarClave(ubicaciones[provincia.value], cantonActual);
        if (claveCanton) canton.value = claveCanton;

        cargarDistritos(provincia, canton, distrito, distritoActual);
    };

    const cargarDistritos = (provincia, canton, distrito, distritoActual = "") => {
        distrito.innerHTML = '<option value="">Seleccione un distrito</option>';
        const lista = ubicaciones[provincia.value]?.[canton.value] || [];
        distrito.disabled = lista.length === 0;
        lista.forEach(nombre => agregarOpcion(distrito, nombre));

        const buscado = normalizar(distritoActual);
        const coincidencia = lista.find(nombre => normalizar(nombre) === buscado);
        if (coincidencia) distrito.value = coincidencia;
    };

    const iniciarGrupo = contenedor => {
        const provincia = contenedor.querySelector('[data-cr-provincia]');
        const canton = contenedor.querySelector('[data-cr-canton]');
        const distrito = contenedor.querySelector('[data-cr-distrito]');
        if (!provincia || !canton || !distrito) return;

        cargarProvincias(provincia, canton, distrito);

        provincia.addEventListener("change", () => {
            canton.dataset.currentValue = "";
            distrito.dataset.currentValue = "";
            cargarCantones(provincia, canton, distrito);
        });

        canton.addEventListener("change", () => {
            distrito.dataset.currentValue = "";
            cargarDistritos(provincia, canton, distrito);
        });
    };

    document.addEventListener("DOMContentLoaded", () => {
        document.querySelectorAll("[data-cr-ubicacion]").forEach(iniciarGrupo);
    });
})();
