```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.08GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|----------:|---------:|------:|--------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 753.9 ns |  23.53 ns | 14.01 ns |  1.03 |    0.02 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 465.8 ns |   4.65 ns |  3.07 ns |  0.64 |    0.01 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 729.8 ns |   9.41 ns |  6.23 ns |  1.00 |    0.01 |         - |          NA |
|                    |            |                |             |          |           |          |       |         |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 734.4 ns |  42.37 ns |  2.32 ns |  1.00 |    0.00 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 465.7 ns | 100.78 ns |  5.52 ns |  0.63 |    0.01 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 738.0 ns |  22.98 ns |  1.26 ns |  1.00 |    0.00 |         - |          NA |
