using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Common.Controls
{
    public interface IRelayCommand : ICommand
    {
        /// <summary>
        /// Notifies that the <see cref="ICommand.CanExecute"/> property has changed.
        /// </summary>
        void NotifyCanExecuteChanged();
    }

    public interface IRelayCommand<in T> : IRelayCommand
    {
        /// <summary>
        /// Provides a strongly-typed variant of <see cref="ICommand.CanExecute(object)"/>.
        /// </summary>
        /// <param name="parameter">The input parameter.</param>
        /// <returns>Whether or not the current command can be executed.</returns>
        /// <remarks>Use this overload to avoid boxing, if <typeparamref name="T"/> is a value type.</remarks>
        bool CanExecute(T parameter);

        /// <summary>
        /// Provides a strongly-typed variant of <see cref="ICommand.Execute(object)"/>.
        /// </summary>
        /// <param name="parameter">The input parameter.</param>
        /// <remarks>Use this overload to avoid boxing, if <typeparamref name="T"/> is a value type.</remarks>
        void Execute(T parameter);
    }

    public class RelayCommand<T> : IRelayCommand<T>
    {
        /// <summary>
        /// The <see cref="Action"/> to invoke when <see cref="Execute(T)"/> is used.
        /// </summary>
        private readonly Action<T> _execute;

        /// <summary>
        /// The optional action to invoke when <see cref="CanExecute(T)"/> is used.
        /// </summary>
        private readonly Predicate<T> _canExecute;

        /// <inheritdoc/>
        public event EventHandler CanExecuteChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="RelayCommand{T}"/> class that can always execute.
        /// </summary>
        /// <param name="execute">The execution logic.</param>
        /// <remarks>
        /// Due to the fact that the <see cref="System.Windows.Input.ICommand"/> interface exposes methods that accept a
        /// nullable <see cref="object"/> parameter, it is recommended that if <typeparamref name="T"/> is a reference type,
        /// you should always declare it as nullable, and to always perform checks within <paramref name="execute"/>.
        /// </remarks>
        /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="execute"/> is <see langword="null"/>.</exception>
        public RelayCommand(Action<T> execute)
        {
            if (execute is null)
            {
                throw new ArgumentNullException(nameof(execute));
            }

            _execute = execute;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RelayCommand{T}"/> class.
        /// </summary>
        /// <param name="execute">The execution logic.</param>
        /// <param name="canExecute">The execution status logic.</param>
        /// <remarks>See notes in <see cref="RelayCommand{T}(System.Action{T?})"/>.</remarks>
        /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="execute"/> or <paramref name="canExecute"/> are <see langword="null"/>.</exception>
        public RelayCommand(Action<T> execute, Predicate<T> canExecute)
        {
            if (execute is null)
            {
                throw new ArgumentNullException(nameof(execute));
            }

            if (canExecute is null)
            {
                throw new ArgumentNullException(nameof(canExecute));
            }

            _execute = execute;
            _canExecute = canExecute;
        }

        /// <inheritdoc/>
        public void NotifyCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CanExecute(T parameter)
        {
            return _canExecute?.Invoke(parameter) != false;
        }

        /// <inheritdoc/>
        public bool CanExecute(object parameter)
        {
            // Special case a null value for a value type argument type.
            // This ensures that no exceptions are thrown during initialization.
            if (parameter is null && (default(T) != null))
            {
                return false;
            }

            if (!TryGetCommandArgument(parameter, out T result))
            {
                ThrowArgumentExceptionForInvalidCommandArgument(parameter);
            }

            return CanExecute(result);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Execute(T parameter)
        {
            _execute(parameter);
        }

        /// <inheritdoc/>
        public void Execute(object parameter)
        {
            if (!TryGetCommandArgument(parameter, out T result))
            {
                ThrowArgumentExceptionForInvalidCommandArgument(parameter);
            }

            Execute(result);
        }

        /// <summary>
        /// Tries to get a command argument of compatible type <typeparamref name="T"/> from an input <see cref="object"/>.
        /// </summary>
        /// <param name="parameter">The input parameter.</param>
        /// <param name="result">The resulting <typeparamref name="T"/> value, if any.</param>
        /// <returns>Whether or not a compatible command argument could be retrieved.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TryGetCommandArgument(object parameter, out T result)
        {
            // If the argument is null and the default value of T is also null, then the
            // argument is valid. T might be a reference type or a nullable value type.
            if (parameter is null && (default(T) == null))
            {
                result = default;

                return true;
            }

            // Check if the argument is a T value, so either an instance of a type or a derived
            // type of T is a reference type, an interface implementation if T is an interface,
            // or a boxed value type in case T was a value type.
            if (parameter is T argument)
            {
                result = argument;

                return true;
            }

            result = default;

            return false;
        }

        /// <summary>
        /// Throws an <see cref="ArgumentException"/> if an invalid command argument is used.
        /// </summary>
        /// <param name="parameter">The input parameter.</param>
        /// <exception cref="ArgumentException">Thrown with an error message to give info on the invalid parameter.</exception>
        internal static void ThrowArgumentExceptionForInvalidCommandArgument(object parameter)
        {
            throw GetException(parameter);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Exception GetException(object parameter) {
            if (parameter is null) {
                return new ArgumentException(
                    $"Parameter \"{nameof(parameter)}\" (object) must not be null, as the command type requires an argument of type {typeof(T)}.",
                    nameof(parameter)
                );
            }

            return new ArgumentException(
                $"Parameter \"{nameof(parameter)}\" (object) cannot be of type {parameter.GetType()}, as the command type requires an argument of type {typeof(T)}.",
                nameof(parameter)
            );
        }
    }
}
