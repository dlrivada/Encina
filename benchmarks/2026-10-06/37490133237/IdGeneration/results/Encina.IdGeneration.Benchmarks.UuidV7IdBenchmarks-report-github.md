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
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 708.9 ns |  17.77 ns | 10.57 ns |  1.04 |    0.02 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 431.8 ns |   8.61 ns |  5.70 ns |  0.63 |    0.01 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 682.2 ns |  14.05 ns |  8.36 ns |  1.00 |    0.02 |         - |          NA |
|                    |            |                |             |          |           |          |       |         |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 679.2 ns | 273.96 ns | 15.02 ns |  1.00 |    0.02 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 441.9 ns |  26.51 ns |  1.45 ns |  0.65 |    0.01 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 681.6 ns | 192.32 ns | 10.54 ns |  1.00 |    0.02 |         - |          NA |
