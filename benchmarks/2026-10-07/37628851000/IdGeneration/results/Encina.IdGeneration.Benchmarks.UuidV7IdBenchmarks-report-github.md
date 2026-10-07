```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 3.37GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|----------:|---------:|------:|--------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 660.3 ns |   6.71 ns |  3.51 ns |  1.00 |    0.01 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 325.8 ns |   0.43 ns |  0.22 ns |  0.49 |    0.00 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 660.7 ns |   6.12 ns |  3.20 ns |  1.00 |    0.01 |         - |          NA |
|                    |            |                |             |          |           |          |       |         |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 680.0 ns | 479.39 ns | 26.28 ns |  1.02 |    0.03 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 332.1 ns | 127.32 ns |  6.98 ns |  0.50 |    0.01 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 665.2 ns |  71.48 ns |  3.92 ns |  1.00 |    0.01 |         - |          NA |
