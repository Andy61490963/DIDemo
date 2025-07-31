using Microsoft.Data.SqlClient;

namespace DynamicForm.Service.Interface.TransactionInterface;

public interface ITransactionService
{
    void WithTransaction(Action<SqlTransaction> action);
    T WithTransaction<T>(Func<SqlTransaction, T> func);
}