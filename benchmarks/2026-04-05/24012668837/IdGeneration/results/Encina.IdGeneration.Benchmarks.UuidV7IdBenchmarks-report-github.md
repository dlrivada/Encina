```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.78GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method             | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean            | Error   | StdDev  | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |------------ |------------- |------------ |----------------:|--------:|--------:|------:|----------:|------------:|
| Generate           | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1,089.0 ns | 3.45 ns | 2.28 ns |  1.00 |         - |          NA |
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1,163.9 ns | 4.52 ns | 2.36 ns |  1.07 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        605.8 ns | 1.43 ns | 0.95 ns |  0.56 |         - |          NA |
|                    |            |                |             |             |              |             |                 |         |         |       |           |             |
| Generate           | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 13,888,056.0 ns |      NA | 0.00 ns |  1.00 |         - |          NA |
| Generate_GetValue  | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 14,043,947.0 ns |      NA | 0.00 ns |  1.01 |         - |          NA |
| NewGuid_Comparison | Dry        | 1              | 1           | ColdStart   | 1            | 1           |    364,500.0 ns |      NA | 0.00 ns |  0.03 |         - |          NA |
