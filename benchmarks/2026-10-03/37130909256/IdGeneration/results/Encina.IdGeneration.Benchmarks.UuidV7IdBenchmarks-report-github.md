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
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 693.6 ns |  33.11 ns | 19.70 ns |  1.01 |    0.03 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 448.0 ns |  14.90 ns |  9.85 ns |  0.65 |    0.02 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 686.2 ns |  17.62 ns |  9.22 ns |  1.00 |    0.02 |         - |          NA |
|                    |            |                |             |          |           |          |       |         |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 671.9 ns |  75.51 ns |  4.14 ns |  1.00 |    0.01 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 440.3 ns |  17.51 ns |  0.96 ns |  0.66 |    0.01 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 670.5 ns | 112.06 ns |  6.14 ns |  1.00 |    0.01 |         - |          NA |
