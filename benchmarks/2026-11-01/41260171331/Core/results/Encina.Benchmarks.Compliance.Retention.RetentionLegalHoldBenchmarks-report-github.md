```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.50GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                     | Mean       | Error    | StdDev   | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |-----------:|---------:|---------:|------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Legal hold: place (cross-aggregate)&#39;      | 1,582.2 ns |  4.84 ns |  7.09 ns |  1.00 |    3 | 0.0572 | 0.0286 |     992 B |        1.00 |
| &#39;Legal hold: lift&#39;                         | 1,400.5 ns | 11.06 ns | 16.20 ns |  0.89 |    2 | 0.0401 | 0.0191 |     712 B |        0.72 |
| &#39;Legal hold: has active holds (read-only)&#39; |   591.0 ns |  3.54 ns |  5.30 ns |  0.37 |    1 | 0.0343 | 0.0172 |     592 B |        0.60 |
