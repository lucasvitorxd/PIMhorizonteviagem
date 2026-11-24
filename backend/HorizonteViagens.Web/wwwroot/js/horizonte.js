// Script do site Horizonte Viagens.
// Ele só cuida da experiência na tela. Nenhuma regra de negócio fica aqui:
// preço, vagas e permissões são decididos no servidor (camada Negocio).
// Tudo fica dentro de uma função que roda sozinha, para as variáveis
// daqui não se misturarem com as de outros scripts da página.
(() => {

    // 1) MENU ☰ NO CELULAR
    // O botão só aparece em telas estreitas (horizonte.css). Cada clique
    // coloca ou tira a classe "aberto" do menu e atualiza o aria-expanded,
    // que é como o leitor de tela sabe se o menu está aberto.
    const botaoMenu = document.querySelector('.menu-toggle');
    if (botaoMenu) {
        const menu = document.getElementById(botaoMenu.getAttribute('aria-controls'));
        botaoMenu.addEventListener('click', () => {
            const aberto = menu.classList.toggle('aberto');
            botaoMenu.setAttribute('aria-expanded', String(aberto));
            botaoMenu.setAttribute('aria-label', aberto ? 'Fechar menu' : 'Abrir menu');
        });
    }

    // 2) NÚMERO DE VIAJANTES
    // Antes, este arquivo recalculava o preço no navegador, repetindo a
    // fórmula da Precificacao. Se a regra mudasse no C#, a tela mostraria
    // um valor diferente do cobrado. Agora ele só reenvia o formulário (GET)
    // e o servidor devolve a página com os valores certos.
    document.querySelectorAll('[data-viajantes]').forEach(select => {
        select.addEventListener('change', () => {
            if (select.form) select.form.submit();
        });
    });

    // 3) PROTEÇÃO CONTRA CLIQUE DUPLO
    // Ao enviar um formulário POST (reservar, salvar, pagar), o botão é
    // desativado para a pessoa não mandar duas vezes. O servidor também se
    // protege (RequisicaoId), isto aqui é só para a pessoa ver que funcionou.
    document.querySelectorAll('form[method="post"]').forEach(form => {
        form.addEventListener('submit', () => {
            // Se o navegador achou campo inválido, o envio não acontece;
            // então o botão continua ativo para a pessoa corrigir.
            if (!form.checkValidity()) return;
            // Nas telas com _ValidationScriptsPartial, quem valida é o jQuery
            // Validation; se ele recusou, também não desativamos o botão.
            if (window.jQuery && jQuery(form).valid && !jQuery(form).valid()) return;
            form.querySelectorAll('button[type="submit"], button:not([type])').forEach(botao => {
                botao.disabled = true;
                botao.textContent = 'Aguarde…';
            });
        });
    });
})();
