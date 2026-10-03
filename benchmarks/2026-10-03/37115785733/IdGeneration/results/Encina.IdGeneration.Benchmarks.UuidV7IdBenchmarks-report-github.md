```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.51GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|----------:|---------:|------:|--------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 665.8 ns |  17.89 ns | 10.64 ns |  1.03 |    0.02 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 422.4 ns |   6.34 ns |  4.19 ns |  0.66 |    0.01 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 643.8 ns |   6.92 ns |  4.58 ns |  1.00 |    0.01 |         - |          NA |
|                    |            |                |             |          |           |          |       |         |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 659.0 ns | 152.90 ns |  8.38 ns |  1.03 |    0.01 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 411.9 ns |  69.74 ns |  3.82 ns |  0.64 |    0.01 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 638.9 ns |  83.56 ns |  4.58 ns |  1.00 |    0.01 |         - |          NA |
