# Conferência da equipe

## Verificações desta revisão

Veja os resultados em [REVISAO-MVC-FLUTTER.md](REVISAO-MVC-FLUTTER.md). Os passos abaixo continuam sendo a conferência necessária no ambiente da equipe.

## Executar no SQL Server da equipe

1. Preparar banco novo com os scripts. Repetir preparação: não deve duplicar destinos nem contas.
2. Conferir `__EFMigrationsHistory`, trigger `TR_Clientes_DataAtualizacao` e procedures `sp_Clientes_BuscarPorNome`/`sp_Combos_BuscarPorDestino`.
3. Alterar cliente e conferir DataAtualizacao. Buscar cliente por nome pela Web (Dapper/procedure).
4. Entrar como admin; criar combo, editar preço/vagas/data e publicar. Excluir combo sem vínculos. Combo com reserva não deve ser apagado; despublicar.
5. Abrir duas abas de edição do mesmo combo; salvar uma e tentar salvar a antiga: deve recusar a versão antiga.
6. Cliente: cadastrar, entrar, editar perfil, favoritar. Cliente não deve acessar gerenciamento nem dados de outro cliente.
7. Confirmar uma oferta controlada com partida futura, reservar duas pessoas e verificar redução de duas vagas. Repetir o mesmo RequisicaoId: somente uma reserva.
8. Conferir grupos de 1/2/3/6 adultos. O servidor recusa total adulterado. Duas compras concorrentes não podem vender mais vagas que o estoque; falha de concorrência deve pedir atualização/tentativa.
9. Cancelar reserva pendente sem checkout e conferir devolução uma única vez. Expirar pendência e conferir devolução. A limpeza ocorre na consulta da lista de reservas; não existe job em background.
10. Com sandbox configurado, abrir checkout, efetuar pagamento de teste e consultar status. Validar aprovação e rejeição com credenciais oficiais. Pix depende da disponibilidade sandbox do provedor. Não use dinheiro real.
11. Admin adiciona um documento HTTPS emitido à reserva confirmada. Cliente vê o mesmo documento em site/Flutter. QR abre o link do fornecedor; não é validação offline antifraude.
12. Devolução/chargeback ou pagamento após expiração deve ir para revisão, sem liberar documentos.

## Limites dos testes

SQLite foi usado exclusivamente como fixture de testes, não como substituição do SQL da entrega. Não valida procedures, trigger T-SQL, SQL Server rowversion nem a concorrência real entre servidores. Esses itens exigem a conferência acima. Nenhum pagamento externo foi executado. Não foi gerado APK nem assinado aplicativo iOS.
