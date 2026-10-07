```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|----------:|---------:|------:|--------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 709.0 ns |  82.73 ns | 54.72 ns |  1.04 |    0.09 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 326.5 ns |   1.53 ns |  0.91 ns |  0.48 |    0.02 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 680.7 ns |  42.20 ns | 27.91 ns |  1.00 |    0.05 |         - |          NA |
|                    |            |                |             |          |           |          |       |         |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 717.9 ns | 574.65 ns | 31.50 ns |  1.08 |    0.04 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 354.5 ns | 456.69 ns | 25.03 ns |  0.54 |    0.03 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 661.8 ns |  17.50 ns |  0.96 ns |  1.00 |    0.00 |         - |          NA |
