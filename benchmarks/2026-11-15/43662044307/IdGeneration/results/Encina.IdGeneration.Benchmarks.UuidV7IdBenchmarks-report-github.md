```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.20GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3


```
| Method             | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Median   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|---------:|------:|--------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 3           | 769.1 ns |  2.12 ns |  1.40 ns | 769.3 ns |  0.96 |    0.00 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 3           | 665.4 ns |  1.77 ns |  1.05 ns | 665.2 ns |  0.83 |    0.00 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 3           | 804.7 ns |  2.22 ns |  1.16 ns | 804.8 ns |  1.00 |    0.00 |         - |          NA |
|                    |            |                |             |             |          |          |          |          |       |         |           |             |
| Generate_GetValue  | MediumRun  | 15             | 2           | 10          | 782.9 ns | 17.10 ns | 23.96 ns | 802.1 ns |  1.01 |    0.03 |         - |          NA |
| NewGuid_Comparison | MediumRun  | 15             | 2           | 10          | 667.1 ns |  2.22 ns |  3.33 ns | 667.4 ns |  0.86 |    0.01 |         - |          NA |
| Generate           | MediumRun  | 15             | 2           | 10          | 772.6 ns |  2.14 ns |  3.00 ns | 771.8 ns |  1.00 |    0.01 |         - |          NA |
