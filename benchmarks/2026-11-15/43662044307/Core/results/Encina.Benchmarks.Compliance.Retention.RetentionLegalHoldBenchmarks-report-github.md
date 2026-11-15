```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                     | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |-----------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Legal hold: place (cross-aggregate)&#39;      | 2,272.5 ns | 18.75 ns | 26.89 ns |  1.00 |    0.02 |    3 | 0.0572 | 0.0267 |    1024 B |        1.00 |
| &#39;Legal hold: lift&#39;                         | 1,976.1 ns | 26.49 ns | 39.65 ns |  0.87 |    0.02 |    2 | 0.0401 | 0.0191 |     712 B |        0.70 |
| &#39;Legal hold: has active holds (read-only)&#39; |   887.1 ns | 15.71 ns | 23.51 ns |  0.39 |    0.01 |    1 | 0.0343 | 0.0172 |     592 B |        0.58 |
