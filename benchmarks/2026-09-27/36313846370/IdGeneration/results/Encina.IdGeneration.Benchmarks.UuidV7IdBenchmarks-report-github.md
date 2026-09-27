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
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 1,164.7 ns |  5.13 ns | 3.39 ns |  1.00 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     |   607.8 ns |  1.13 ns | 0.59 ns |  0.52 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 1,161.7 ns |  7.32 ns | 4.84 ns |  1.00 |         - |          NA |
|                    |            |                |             |            |          |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 1,084.2 ns | 36.33 ns | 1.99 ns |  1.01 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           |   608.3 ns |  8.06 ns | 0.44 ns |  0.57 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 1,074.8 ns | 53.27 ns | 2.92 ns |  1.00 |         - |          NA |
