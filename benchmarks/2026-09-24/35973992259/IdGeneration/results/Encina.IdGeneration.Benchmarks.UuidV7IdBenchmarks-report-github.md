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
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 1,091.0 ns |  4.40 ns | 2.91 ns |  1.02 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     |   609.3 ns |  0.63 ns | 0.33 ns |  0.57 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 1,068.5 ns |  3.77 ns | 2.24 ns |  1.00 |         - |          NA |
|                    |            |                |             |            |          |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 1,075.2 ns | 32.17 ns | 1.76 ns |  1.00 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           |   607.8 ns | 38.29 ns | 2.10 ns |  0.57 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 1,072.5 ns | 31.92 ns | 1.75 ns |  1.00 |         - |          NA |
