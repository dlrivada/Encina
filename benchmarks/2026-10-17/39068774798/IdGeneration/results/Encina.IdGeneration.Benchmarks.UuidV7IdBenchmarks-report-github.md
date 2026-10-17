```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.74GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean       | Error    | StdDev  | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |-----------:|---------:|--------:|------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     |   983.0 ns |  7.32 ns | 4.84 ns |  0.99 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     |   397.0 ns |  0.31 ns | 0.20 ns |  0.40 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     |   991.9 ns |  2.42 ns | 1.60 ns |  1.00 |         - |          NA |
|                    |            |                |             |            |          |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 1,048.6 ns | 49.35 ns | 2.70 ns |  1.07 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           |   395.9 ns |  2.21 ns | 0.12 ns |  0.41 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           |   977.4 ns | 33.73 ns | 1.85 ns |  1.00 |         - |          NA |
