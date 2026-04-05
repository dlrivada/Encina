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
| &#39;FluentValidation (Valid)&#39; |  94.69 ms |    NA |  1.00 |    1 |  22.38 KB |        1.00 |
| &#39;DataAnnotations (Valid)&#39;  |  95.31 ms |    NA |  1.01 |    3 |   20.7 KB |        0.92 |
| &#39;MiniValidator (Valid)&#39;    |  95.26 ms |    NA |  1.01 |    2 |  20.67 KB |        0.92 |
| &#39;GuardClauses (Valid)&#39;     | 102.80 ms |    NA |  1.09 |    4 |  19.89 KB |        0.89 |
