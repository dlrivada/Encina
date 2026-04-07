```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4


```
| Method             | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean            | Error   | StdDev  | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |------------ |------------- |------------ |----------------:|--------:|--------:|------:|----------:|------------:|
| Generate           | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        967.4 ns | 3.05 ns | 1.81 ns |  1.00 |         - |          NA |
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        964.8 ns | 2.49 ns | 1.65 ns |  1.00 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        395.5 ns | 0.27 ns | 0.18 ns |  0.41 |         - |          NA |
|                    |            |                |             |             |              |             |                 |         |         |       |           |             |
| Generate           | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 12,904,187.0 ns |      NA | 0.00 ns |  1.00 |         - |          NA |
| Generate_GetValue  | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 12,955,705.0 ns |      NA | 0.00 ns |  1.00 |         - |          NA |
| NewGuid_Comparison | Dry        | 1              | 1           | ColdStart   | 1            | 1           |    320,618.0 ns |      NA | 0.00 ns |  0.02 |         - |          NA |
