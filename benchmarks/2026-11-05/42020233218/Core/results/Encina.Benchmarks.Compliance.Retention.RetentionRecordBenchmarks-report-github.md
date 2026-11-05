```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                               | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------- |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Record: track entity&#39;               | 3.454 μs | 1.4103 μs | 0.0773 μs |  1.00 |    0.03 |    2 | 0.0648 | 0.0305 |    1088 B |        1.00 |
| &#39;Record: mark expired&#39;               | 1.879 μs | 0.4562 μs | 0.0250 μs |  0.54 |    0.01 |    1 | 0.0343 | 0.0153 |     608 B |        0.56 |
| &#39;Record: mark deleted (terminal)&#39;    | 1.938 μs | 0.6589 μs | 0.0361 μs |  0.56 |    0.01 |    1 | 0.0362 | 0.0172 |     608 B |        0.56 |
| &#39;Record: mark anonymized (terminal)&#39; | 1.895 μs | 0.4884 μs | 0.0268 μs |  0.55 |    0.01 |    1 | 0.0362 | 0.0172 |     608 B |        0.56 |
