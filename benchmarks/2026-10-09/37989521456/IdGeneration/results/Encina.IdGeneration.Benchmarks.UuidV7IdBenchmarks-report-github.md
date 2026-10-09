```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error    | StdDev  | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|---------:|--------:|------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 983.4 ns |  3.19 ns | 2.11 ns |  1.00 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 401.8 ns |  0.64 ns | 0.34 ns |  0.41 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 983.7 ns |  1.90 ns | 1.26 ns |  1.00 |         - |          NA |
|                    |            |                |             |          |          |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 981.7 ns | 21.44 ns | 1.18 ns |  1.00 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 396.5 ns |  9.19 ns | 0.50 ns |  0.40 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 983.8 ns | 14.17 ns | 0.78 ns |  1.00 |         - |          NA |
