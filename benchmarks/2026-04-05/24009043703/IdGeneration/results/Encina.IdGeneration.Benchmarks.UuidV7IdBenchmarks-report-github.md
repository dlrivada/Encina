```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method             | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean            | Error   | StdDev  | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |------------ |------------- |------------ |----------------:|--------:|--------:|------:|----------:|------------:|
| Generate           | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1,078.3 ns | 2.47 ns | 1.63 ns |  1.00 |         - |          NA |
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1,079.0 ns | 3.69 ns | 2.20 ns |  1.00 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        615.1 ns | 8.15 ns | 5.39 ns |  0.57 |         - |          NA |
|                    |            |                |             |             |              |             |                 |         |         |       |           |             |
| Generate           | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 13,618,647.0 ns |      NA | 0.00 ns |  1.00 |         - |          NA |
| Generate_GetValue  | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 13,832,486.0 ns |      NA | 0.00 ns |  1.02 |         - |          NA |
| NewGuid_Comparison | Dry        | 1              | 1           | ColdStart   | 1            | 1           |    314,419.0 ns |      NA | 0.00 ns |  0.02 |         - |          NA |
