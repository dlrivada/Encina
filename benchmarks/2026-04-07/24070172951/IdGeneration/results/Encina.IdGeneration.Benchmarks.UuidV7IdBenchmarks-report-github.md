```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 3.15GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev  | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|----------:|--------:|------:|----------:|------------:|
| Generate           | Job-YFEFPZ | 10             | Default     | 965.2 ns |   1.25 ns | 0.65 ns |  1.00 |         - |          NA |
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 968.6 ns |  10.91 ns | 7.22 ns |  1.00 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 396.0 ns |   0.69 ns | 0.41 ns |  0.41 |         - |          NA |
|                    |            |                |             |          |           |         |       |           |             |
| Generate           | ShortRun   | 3              | 1           | 990.2 ns | 180.36 ns | 9.89 ns |  1.00 |         - |          NA |
| Generate_GetValue  | ShortRun   | 3              | 1           | 961.4 ns |  11.59 ns | 0.64 ns |  0.97 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 395.6 ns |   7.32 ns | 0.40 ns |  0.40 |         - |          NA |
