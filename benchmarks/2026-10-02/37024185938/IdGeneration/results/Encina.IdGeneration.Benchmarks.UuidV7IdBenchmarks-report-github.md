```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean       | Error       | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |-----------:|------------:|---------:|------:|--------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 1,129.3 ns |     1.51 ns |  0.90 ns |  0.99 |    0.00 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     |   718.5 ns |     0.84 ns |  0.44 ns |  0.63 |    0.00 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 1,142.8 ns |     8.78 ns |  4.59 ns |  1.00 |    0.01 |         - |          NA |
|                    |            |                |             |            |             |          |       |         |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 1,177.5 ns | 1,278.67 ns | 70.09 ns |  1.04 |    0.05 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           |   719.7 ns |    32.20 ns |  1.76 ns |  0.64 |    0.00 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 1,127.2 ns |    36.69 ns |  2.01 ns |  1.00 |    0.00 |         - |          NA |
