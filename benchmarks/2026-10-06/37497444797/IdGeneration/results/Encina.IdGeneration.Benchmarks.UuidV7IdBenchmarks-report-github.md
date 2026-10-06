```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|----------:|---------:|------:|--------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 740.0 ns |  20.78 ns | 13.74 ns |  1.03 |    0.02 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 448.8 ns |   4.54 ns |  3.01 ns |  0.62 |    0.00 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 718.7 ns |   4.80 ns |  2.86 ns |  1.00 |    0.01 |         - |          NA |
|                    |            |                |             |          |           |          |       |         |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 718.2 ns | 363.44 ns | 19.92 ns |  1.00 |    0.02 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 449.1 ns |  55.29 ns |  3.03 ns |  0.62 |    0.00 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 721.7 ns |  66.30 ns |  3.63 ns |  1.00 |    0.01 |         - |          NA |
