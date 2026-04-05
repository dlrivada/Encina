```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

```
| Method                     | Mean      | Error | Ratio | Rank | Allocated | Alloc Ratio |
|--------------------------- |----------:|------:|------:|-----:|----------:|------------:|
| &#39;FluentValidation (Valid)&#39; |  97.88 ms |    NA |  1.00 |    1 |  19.89 KB |        1.00 |
| &#39;DataAnnotations (Valid)&#39;  |  99.10 ms |    NA |  1.01 |    3 |  19.96 KB |        1.00 |
| &#39;MiniValidator (Valid)&#39;    |  98.01 ms |    NA |  1.00 |    2 |  21.28 KB |        1.07 |
| &#39;GuardClauses (Valid)&#39;     | 107.13 ms |    NA |  1.09 |    4 |   22.9 KB |        1.15 |
