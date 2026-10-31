```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|-------------------------------- |----------:|----------:|---------:|------:|--------:|--------:|-------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  27.08 μs |  1.776 μs | 0.097 μs |  1.00 |    0.00 |  1.0376 | 0.0305 |  17.12 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  43.50 μs | 42.859 μs | 2.349 μs |  1.61 |    0.08 |  1.1597 | 0.0610 |   19.5 KB |        1.14 |
| Invalidation_SequentialCommands | 280.73 μs | 20.697 μs | 1.134 μs | 10.37 |    0.05 | 10.2539 |      - |  171.7 KB |       10.03 |
