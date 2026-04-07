```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method             | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |------------ |-----------:|---------:|---------:|------:|--------:|----------:|------------:|
| Generate           | Job-YFEFPZ | 10             | Default     | 3           | 1,070.9 ns |  3.90 ns |  2.04 ns |  1.00 |    0.00 |         - |          NA |
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 3           | 1,084.8 ns |  5.62 ns |  3.34 ns |  1.01 |    0.00 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 3           |   602.7 ns |  1.04 ns |  0.62 ns |  0.56 |    0.00 |         - |          NA |
|                    |            |                |             |             |            |          |          |       |         |           |             |
| Generate           | MediumRun  | 15             | 2           | 10          | 1,081.5 ns |  3.62 ns |  5.19 ns |  1.00 |    0.01 |         - |          NA |
| Generate_GetValue  | MediumRun  | 15             | 2           | 10          | 1,110.6 ns | 32.81 ns | 44.92 ns |  1.03 |    0.04 |         - |          NA |
| NewGuid_Comparison | MediumRun  | 15             | 2           | 10          |   604.6 ns |  0.71 ns |  1.01 ns |  0.56 |    0.00 |         - |          NA |
