using System.Collections.Generic;

namespace FoodOrderingSystem.Application.Common.Models
{
    public class Result
    {
        internal Result(bool succeeded, IEnumerable<string> errors)
        {
            Succeeded = succeeded;
            Errors = errors;
        }

        public bool Succeeded { get; set; }
        public IEnumerable<string> Errors { get; set; }

        public static Result Success()
        {
            return new Result(true, new string[] { });
        }

        public static Result Failure(IEnumerable<string> errors)
        {
            return new Result(false, errors);
        }

        public static Result Failure(string error)
        {
            return new Result(false, new string[] { error });
        }
    }

    public class Result<T> : Result
    {
        internal Result(bool succeeded, T? data, IEnumerable<string> errors)
            : base(succeeded, errors)
        {
            Data = data;
        }

        public T? Data { get; set; }

        public static Result<T> Success(T data)
        {
            return new Result<T>(true, data, new string[] { });
        }

        public new static Result<T> Failure(IEnumerable<string> errors)
        {
            return new Result<T>(false, default, errors);
        }

        public new static Result<T> Failure(string error)
        {
            return new Result<T>(false, default, new string[] { error });
        }
    }
} 