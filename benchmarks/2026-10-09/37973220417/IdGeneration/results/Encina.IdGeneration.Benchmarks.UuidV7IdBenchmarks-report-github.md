```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev  | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|----------:|--------:|------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 976.0 ns |   2.68 ns | 1.77 ns |  1.06 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 559.8 ns |   1.92 ns | 1.00 ns |  0.61 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 917.2 ns |   4.81 ns | 2.52 ns |  1.00 |         - |          NA |
|                    |            |                |             |          |           |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 917.2 ns | 178.58 ns | 9.79 ns |  1.01 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 559.0 ns |   6.51 ns | 0.36 ns |  0.61 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 909.2 ns |  96.72 ns | 5.30 ns |  1.00 |         - |          NA |
