```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.27GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                     | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |-----------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Legal hold: place (cross-aggregate)&#39;      | 1,696.0 ns | 39.55 ns | 59.20 ns |  1.00 |    0.05 |    3 | 0.0572 | 0.0286 |     992 B |        1.00 |
| &#39;Legal hold: lift&#39;                         | 1,567.8 ns | 32.64 ns | 46.81 ns |  0.93 |    0.04 |    2 | 0.0401 | 0.0191 |     712 B |        0.72 |
| &#39;Legal hold: has active holds (read-only)&#39; |   704.2 ns | 27.16 ns | 39.80 ns |  0.42 |    0.03 |    1 | 0.0343 | 0.0172 |     592 B |        0.60 |
