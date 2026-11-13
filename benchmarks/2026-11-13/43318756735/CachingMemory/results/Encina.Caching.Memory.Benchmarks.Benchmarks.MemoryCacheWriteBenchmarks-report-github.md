```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.73GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 3.399 μs | 0.3219 μs | 0.0176 μs |  0.88 |    0.07 | 0.0458 |      - |     784 B |        1.10 |
| SetWithSlidingExpirationAsync | 3.938 μs | 9.1630 μs | 0.5023 μs |  1.02 |    0.14 | 0.0420 | 0.0381 |     720 B |        1.01 |
| SetAsync                      | 3.880 μs | 6.0745 μs | 0.3330 μs |  1.01 |    0.11 | 0.0420 | 0.0381 |     712 B |        1.00 |
