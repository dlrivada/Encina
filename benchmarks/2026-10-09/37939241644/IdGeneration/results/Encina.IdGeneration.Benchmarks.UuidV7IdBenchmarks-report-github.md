```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev   | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|----------:|---------:|------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 930.7 ns |   4.50 ns |  2.98 ns |  1.01 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 568.8 ns |  16.34 ns | 10.81 ns |  0.62 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 921.3 ns |   3.00 ns |  1.57 ns |  1.00 |         - |          NA |
|                    |            |                |             |          |           |          |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 911.0 ns |  91.93 ns |  5.04 ns |  0.93 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 558.2 ns |  27.21 ns |  1.49 ns |  0.57 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 977.9 ns | 162.68 ns |  8.92 ns |  1.00 |         - |          NA |
