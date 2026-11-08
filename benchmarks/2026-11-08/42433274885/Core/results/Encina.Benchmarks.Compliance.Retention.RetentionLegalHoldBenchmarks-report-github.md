```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.65GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                     | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |-----------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Legal hold: place (cross-aggregate)&#39;      | 2,249.6 ns | 16.58 ns | 24.81 ns |  1.00 |    0.02 |    3 | 0.0572 | 0.0267 |    1024 B |        1.00 |
| &#39;Legal hold: lift&#39;                         | 1,900.9 ns | 18.96 ns | 28.38 ns |  0.85 |    0.02 |    2 | 0.0401 | 0.0191 |     712 B |        0.70 |
| &#39;Legal hold: has active holds (read-only)&#39; |   868.3 ns |  9.74 ns | 14.58 ns |  0.39 |    0.01 |    1 | 0.0343 | 0.0172 |     592 B |        0.58 |
