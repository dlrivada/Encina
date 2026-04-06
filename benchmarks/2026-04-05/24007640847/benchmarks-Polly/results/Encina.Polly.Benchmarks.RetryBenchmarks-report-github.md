```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-NTRUNJ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                             | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean            | Error    | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------------------------- |----------- |--------------- |------------ |------------ |------------- |------------ |----------------:|---------:|--------:|------:|-------:|----------:|------------:|
| NoRetryAttribute_Baseline          | Job-NTRUNJ | 5              | Default     | Default     | 16           | 3           |        784.6 ns |  8.01 ns | 2.08 ns |  1.00 | 0.0525 |     880 B |        1.00 |
| WithRetryAttribute_NoActualRetries | Job-NTRUNJ | 5              | Default     | Default     | 16           | 3           |        792.5 ns | 12.94 ns | 3.36 ns |  1.01 | 0.0525 |     880 B |        1.00 |
|                                    |            |                |             |             |              |             |                 |          |         |       |        |           |             |
| NoRetryAttribute_Baseline          | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 41,933,883.0 ns |       NA | 0.00 ns |  1.00 |      - |    1584 B |        1.00 |
| WithRetryAttribute_NoActualRetries | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 41,585,591.0 ns |       NA | 0.00 ns |  0.99 |      - |    1568 B |        0.99 |
