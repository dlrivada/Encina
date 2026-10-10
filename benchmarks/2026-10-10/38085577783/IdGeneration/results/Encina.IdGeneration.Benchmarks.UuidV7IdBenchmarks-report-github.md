```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |-----------:|----------:|---------:|------:|--------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 1,205.2 ns |   3.81 ns |  2.52 ns |  1.06 |    0.00 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     |   610.3 ns |   0.65 ns |  0.43 ns |  0.54 |    0.00 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 1,137.4 ns |   5.33 ns |  3.53 ns |  1.00 |    0.00 |         - |          NA |
|                    |            |                |             |            |           |          |       |         |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 1,229.5 ns | 759.80 ns | 41.65 ns |  1.08 |    0.03 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           |   607.0 ns |   6.49 ns |  0.36 ns |  0.53 |    0.00 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 1,139.6 ns |  13.89 ns |  0.76 ns |  1.00 |    0.00 |         - |          NA |
