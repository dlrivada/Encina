```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.202
  [Host]     : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3


```
| Method             | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error    | StdDev   | Median     | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |------------ |-----------:|---------:|---------:|-----------:|------:|--------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 3           | 1,068.2 ns | 12.85 ns |  7.65 ns | 1,067.5 ns |  1.02 |    0.01 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 3           |   604.7 ns |  3.22 ns |  2.13 ns |   604.9 ns |  0.58 |    0.01 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 3           | 1,051.1 ns | 15.04 ns |  9.95 ns | 1,055.3 ns |  1.00 |    0.01 |         - |          NA |
|                    |            |                |             |             |            |          |          |            |       |         |           |             |
| Generate_GetValue  | MediumRun  | 15             | 2           | 10          | 1,088.0 ns |  8.12 ns | 11.91 ns | 1,081.6 ns |  0.99 |    0.03 |         - |          NA |
| NewGuid_Comparison | MediumRun  | 15             | 2           | 10          |   606.3 ns |  0.59 ns |  0.87 ns |   606.3 ns |  0.55 |    0.02 |         - |          NA |
| Generate           | MediumRun  | 15             | 2           | 10          | 1,102.7 ns | 25.02 ns | 37.44 ns | 1,097.7 ns |  1.00 |    0.05 |         - |          NA |
