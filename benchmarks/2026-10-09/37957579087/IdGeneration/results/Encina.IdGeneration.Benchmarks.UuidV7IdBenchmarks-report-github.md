```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean       | Error       | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |-----------:|------------:|---------:|------:|--------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 1,134.3 ns |     3.74 ns |  2.47 ns |  0.94 |    0.00 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     |   609.9 ns |     1.35 ns |  0.80 ns |  0.50 |    0.00 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 1,211.4 ns |     2.69 ns |  1.60 ns |  1.00 |    0.00 |         - |          NA |
|                    |            |                |             |            |             |          |       |         |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 1,252.1 ns | 1,008.30 ns | 55.27 ns |  1.03 |    0.04 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           |   607.5 ns |     1.88 ns |  0.10 ns |  0.50 |    0.00 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 1,218.7 ns |   132.75 ns |  7.28 ns |  1.00 |    0.01 |         - |          NA |
