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
| &#39;FluentValidation (Valid)&#39; |  99.70 ms |    NA |  1.00 |    3 |  22.38 KB |        1.00 |
| &#39;DataAnnotations (Valid)&#39;  |  98.60 ms |    NA |  0.99 |    2 |  19.96 KB |        0.89 |
| &#39;MiniValidator (Valid)&#39;    |  98.58 ms |    NA |  0.99 |    1 |  19.07 KB |        0.85 |
| &#39;GuardClauses (Valid)&#39;     | 107.95 ms |    NA |  1.08 |    4 |  20.91 KB |        0.93 |
