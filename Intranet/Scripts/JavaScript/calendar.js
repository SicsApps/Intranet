// Espera ate que todo o conteudo da pagina (HTML) seja carregado
document.addEventListener('DOMContentLoaded', () => {

    // Pega o elemento onde o calendario vai ser exibido
    const calendarEl = document.getElementById('calendar');

    // Pega o elemento onde vai aparecer a legenda dos feriados
    const legendEl = document.getElementById('holiday-legend');

    // Lista com todos os feriados de 2026 (FIEMG / MG)
    // Cada objeto tem: titulo, data e tipo (nacional ou municipal)
    const holidays = [
        { title: 'Confraternizacao Universal', date: '2026-01-01', type: 'nacional' },
        { title: 'Carnaval (Segunda)', date: '2026-02-16', type: 'nacional' },
        { title: 'Carnaval (Terca)', date: '2026-02-17', type: 'nacional' },
        { title: 'Quarta-feira de Cinzas', date: '2026-02-18', type: 'nacional' },
        { title: 'Sexta-feira Santa', date: '2026-04-03', type: 'nacional' },
        { title: 'Tiradentes', date: '2026-04-21', type: 'nacional' },
        { title: 'Dia do Trabalho', date: '2026-05-01', type: 'nacional' },
        { title: 'Corpus Christi', date: '2026-06-04', type: 'nacional' },
        { title: 'Independencia do Brasil', date: '2026-09-07', type: 'nacional' },
        { title: 'Nossa Senhora Aparecida', date: '2026-10-12', type: 'nacional' },
        { title: 'Finados', date: '2026-11-02', type: 'nacional' },
        { title: 'Proclamacao da Republica', date: '2026-11-15', type: 'nacional' },
        { title: 'Dia da Consciencia Negra', date: '2026-11-20', type: 'nacional' },
        { title: 'Natal', date: '2026-12-25', type: 'nacional' },
        { title: 'Assuncao de Nossa Senhora', date: '2026-08-15', type: 'municipal' },
        { title: 'Imaculada Conceicao', date: '2026-12-08', type: 'municipal' }
    ];

    // Cria o calendario usando a biblioteca FullCalendar
    const calendar = new FullCalendar.Calendar(calendarEl, {
        // Define o idioma como portugues do Brasil
        locale: 'pt-br',

        // Define o tipo de visualizacao inicial
        initialView: 'dayGridMonth',

        // Traducao do botao "today"
        buttonText: { today: 'Hoje' },

        // Adiciona os feriados como eventos
        events: holidays.map(h => ({
            title: h.title,
            start: h.date,
            className: h.type
        })),

        // Atualiza a legenda ao trocar de mes
        datesSet: ({ view }) => updateLegend(view.currentStart)
    });

    // Renderiza o calendario
    calendar.render();

    // Atualiza a legenda
    function updateLegend(currentDate) {
        const month = currentDate.getMonth() + 1;
        const year = currentDate.getFullYear();

        const currentHolidays = holidays.filter(h => {
            const [y, m] = h.date.split('-').map(Number);
            return y === year && m === month;
        });

        legendEl.innerHTML = currentHolidays.length
            ? currentHolidays.map(h => {
                const [y, m, d] = h.date.split('-');
                return `<li class="${h.type}">${h.title} - ${d}/${m}/${y}</li>`;
            }).join('')
            : '<li>Sem feriados neste mes</li>';
    }

    // Atualiza a legenda ao carregar
    updateLegend(new Date());
});
