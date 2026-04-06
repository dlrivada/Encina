```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

```
| Method                                                    | Mean        | Error | Ratio | Rank | Allocated | Alloc Ratio |
|---------------------------------------------------------- |------------:|------:|------:|-----:|----------:|------------:|
| &#39;Serialize PolicySet (Small: 1 policy, 2 rules)&#39;          |    876.4 μs |    NA |  1.00 |    4 |    2064 B |        1.00 |
| &#39;Serialize PolicySet (Medium: 3 policies, 15 rules)&#39;      |    709.3 μs |    NA |  0.81 |    2 |   22464 B |       10.88 |
| &#39;Serialize PolicySet (Large: 10 policies, 100 rules)&#39;     |  1,779.4 μs |    NA |  2.03 |    5 |  402480 B |      195.00 |
| &#39;Serialize Policy (Small: 2 rules, no target)&#39;            |    639.9 μs |    NA |  0.73 |    1 |    1000 B |        0.48 |
| &#39;Serialize Policy (Large: 10 rules, target + conditions)&#39; |    719.7 μs |    NA |  0.82 |    3 |   37008 B |       17.93 |
| &#39;Deserialize PolicySet (Small)&#39;                           | 36,053.1 μs |    NA | 41.14 |    8 |    2872 B |        1.39 |
| &#39;Deserialize PolicySet (Medium)&#39;                          | 41,529.5 μs |    NA | 47.39 |    9 |   35952 B |       17.42 |
| &#39;Deserialize PolicySet (Large)&#39;                           | 57,104.9 μs |    NA | 65.16 |   11 |  711008 B |      344.48 |
| &#39;Deserialize Policy (Small)&#39;                              | 34,743.9 μs |    NA | 39.64 |    6 |    1424 B |        0.69 |
| &#39;Deserialize Policy (Large)&#39;                              | 52,087.1 μs |    NA | 59.43 |   10 |   65816 B |       31.89 |
| &#39;Round-Trip PolicySet (Small)&#39;                            | 35,707.9 μs |    NA | 40.74 |    7 |    4936 B |        2.39 |
| &#39;Round-Trip PolicySet (Large)&#39;                            | 57,399.6 μs |    NA | 65.50 |   12 | 1113016 B |      539.25 |
