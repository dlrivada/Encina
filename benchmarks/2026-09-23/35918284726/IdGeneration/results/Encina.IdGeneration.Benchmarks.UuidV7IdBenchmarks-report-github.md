```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean       | Error    | StdDev  | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |-----------:|---------:|--------:|------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 1,158.0 ns |  5.33 ns | 3.53 ns |  1.08 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     |   609.3 ns |  0.76 ns | 0.45 ns |  0.57 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 1,073.9 ns |  2.37 ns | 1.57 ns |  1.00 |         - |          NA |
|                    |            |                |             |            |          |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 1,147.0 ns | 28.91 ns | 1.58 ns |  1.07 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           |   609.5 ns |  4.34 ns | 0.24 ns |  0.57 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 1,073.1 ns | 33.85 ns | 1.86 ns |  1.00 |         - |          NA |
