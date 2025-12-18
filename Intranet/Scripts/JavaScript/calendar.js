// Espera até que todo o conteúdo da página (HTML) seja carregado
document.addEventListener('DOMContentLoaded', () => {

    // Pega o elemento onde o calendário vai ser exibido
    const calendarEl = document.getElementById('calendar');

    // Pega o elemento onde vai aparecer a legenda dos feriados
    const legendEl = document.getElementById('holiday-legend');

    // Lista com todos os feriados de 2025
    // Cada objeto tem: título, data e tipo (nacional ou municipal)
    const holidays = [

    ];


    // Cria o calendário usando a biblioteca FullCalendar
    const calendar = new FullCalendar.Calendar(calendarEl, {
        // Define o idioma como português do Brasil
        locale: 'pt-br',

        // Define o tipo de visualização inicial (mês em grade)
        initialView: 'dayGridMonth',

        // Tradução do botão "today" para "Hoje"
        buttonText: { today: 'Hoje' },

        // Adiciona os feriados como eventos no calendário
        events: holidays.map(h => ({
            title: h.title,  // Nome do feriado
            start: h.date,   // Data
            className: h.type // Classe CSS (pode ser usada pra estilizar nacional x municipal)
        })),

        // Toda vez que o usuário muda de mês, chama a função updateLegend()
        // pra atualizar a lista de feriados daquele mês
        datesSet: ({ view }) => updateLegend(view.currentStart)
    });

    // Renderiza o calendário na tela
    calendar.render();

    // Função que atualiza a legenda (lista de feriados do mês atual)
    function updateLegend(currentDate) {
        // Pega o mês e o ano atuais do calendário
        const month = currentDate.getMonth() + 1; // +1 porque os meses vão de 0 a 11
        const year = currentDate.getFullYear();

        // Filtra apenas os feriados que pertencem ao mês e ano atuais
        const currentHolidays = holidays.filter(h => {
            const [y, m] = h.date.split('-').map(Number);
            return y === year && m === month;
        });

        // Monta o HTML da lista de feriados
        // Se houver feriados, lista cada um com data formatada
        // Caso contrário, mostra "Sem feriados neste mês"
        legendEl.innerHTML = currentHolidays.length
            ? currentHolidays.map(h => {
                const [y, m, d] = h.date.split('-');
                return `<li class="${h.type}">${h.title} - ${d}/${m}/${y}</li>`;
            }).join('')
            : '<li>Sem feriados neste mes</li>';
    }

    // Atualiza a legenda logo que o calendário é carregado (mês atual)
    updateLegend(new Date());
});