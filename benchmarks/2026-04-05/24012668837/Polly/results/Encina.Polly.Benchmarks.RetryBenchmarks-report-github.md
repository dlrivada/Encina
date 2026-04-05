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
| NoRetryAttribute_Baseline          | Job-NTRUNJ | 5              | Default     | Default     | 16           | 3           |        829.8 ns | 33.07 ns | 8.59 ns |  1.00 | 0.0525 |     880 B |        1.00 |
| WithRetryAttribute_NoActualRetries | Job-NTRUNJ | 5              | Default     | Default     | 16           | 3           |        811.2 ns | 10.29 ns | 1.59 ns |  0.98 | 0.0525 |     880 B |        1.00 |
|                                    |            |                |             |             |              |             |                 |          |         |       |        |           |             |
| NoRetryAttribute_Baseline          | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 43,709,706.0 ns |       NA | 0.00 ns |  1.00 |      - |    1584 B |        1.00 |
| WithRetryAttribute_NoActualRetries | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 43,530,623.0 ns |       NA | 0.00 ns |  1.00 |      - |    1568 B |        0.99 |
