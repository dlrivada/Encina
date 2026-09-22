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
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 1,140.6 ns |  4.08 ns | 2.70 ns |  1.06 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     |   608.5 ns |  1.96 ns | 1.30 ns |  0.57 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 1,075.1 ns |  3.11 ns | 2.06 ns |  1.00 |         - |          NA |
|                    |            |                |             |            |          |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 1,155.8 ns | 24.45 ns | 1.34 ns |  1.06 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           |   608.0 ns |  1.52 ns | 0.08 ns |  0.56 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 1,087.9 ns | 54.78 ns | 3.00 ns |  1.00 |         - |          NA |
