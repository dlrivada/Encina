```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev   | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |-----------:|----------:|---------:|------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 1,085.7 ns |   2.36 ns |  1.40 ns |  1.00 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     |   609.6 ns |   1.20 ns |  0.63 ns |  0.56 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 1,080.8 ns |   3.49 ns |  2.08 ns |  1.00 |         - |          NA |
|                    |            |                |             |            |           |          |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 1,175.6 ns | 324.84 ns | 17.81 ns |  1.01 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           |   608.6 ns |   2.41 ns |  0.13 ns |  0.52 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 1,167.9 ns | 112.23 ns |  6.15 ns |  1.00 |         - |          NA |
