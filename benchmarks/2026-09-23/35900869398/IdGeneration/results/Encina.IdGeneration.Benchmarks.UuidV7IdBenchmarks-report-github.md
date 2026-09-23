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
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 1,121.6 ns |     3.60 ns |  2.38 ns |  1.00 |    0.00 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     |   721.4 ns |     1.61 ns |  0.96 ns |  0.64 |    0.00 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 1,124.6 ns |     1.98 ns |  1.18 ns |  1.00 |    0.00 |         - |          NA |
|                    |            |                |             |            |             |          |       |         |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 1,171.7 ns | 1,136.29 ns | 62.28 ns |  1.05 |    0.05 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           |   716.7 ns |     4.08 ns |  0.22 ns |  0.64 |    0.00 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 1,118.1 ns |    21.84 ns |  1.20 ns |  1.00 |    0.00 |         - |          NA |
