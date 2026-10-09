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
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 1,239.1 ns |  5.09 ns | 3.03 ns |  1.03 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     |   610.4 ns |  2.25 ns | 1.49 ns |  0.51 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 1,203.5 ns |  6.85 ns | 4.53 ns |  1.00 |         - |          NA |
|                    |            |                |             |            |          |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 1,167.4 ns | 28.85 ns | 1.58 ns |  1.05 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           |   608.8 ns | 16.35 ns | 0.90 ns |  0.55 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 1,111.4 ns | 54.72 ns | 3.00 ns |  1.00 |         - |          NA |
