```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.202
  [Host]    : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                          | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------------------------------------ |-----------:|---------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| &#39;Validate: compliant request (fast path)&#39;       |   656.0 ns |  6.75 ns | 10.10 ns |  1.00 |    0.02 |    3 | 0.0353 |     600 B |        1.00 |
| &#39;Validate: non-compliant (violations detected)&#39; | 1,314.9 ns |  8.04 ns | 12.03 ns |  2.00 |    0.04 |    5 | 0.0954 |    1608 B |        2.68 |
| &#39;Analyze: data minimization only&#39;               |   553.6 ns |  8.52 ns | 12.76 ns |  0.84 |    0.02 |    2 | 0.0534 |     896 B |        1.49 |
| &#39;Validate: default privacy inspection&#39;          |   280.3 ns |  6.56 ns |  9.82 ns |  0.43 |    0.02 |    1 | 0.0167 |     280 B |        0.47 |
| &#39;Analyze: large field count (15 properties)&#39;    | 1,207.5 ns | 17.89 ns | 26.78 ns |  1.84 |    0.05 |    4 | 0.1335 |    2264 B |        3.77 |
