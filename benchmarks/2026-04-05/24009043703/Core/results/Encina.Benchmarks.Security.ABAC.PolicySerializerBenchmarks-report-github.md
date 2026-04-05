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
| &#39;Serialize PolicySet (Small: 1 policy, 2 rules)&#39;          |    635.6 μs |    NA |  1.00 |    1 |    2064 B |        1.00 |
| &#39;Serialize PolicySet (Medium: 3 policies, 15 rules)&#39;      |    702.1 μs |    NA |  1.10 |    3 |   22464 B |       10.88 |
| &#39;Serialize PolicySet (Large: 10 policies, 100 rules)&#39;     |  1,781.2 μs |    NA |  2.80 |    5 |  402480 B |      195.00 |
| &#39;Serialize Policy (Small: 2 rules, no target)&#39;            |    719.0 μs |    NA |  1.13 |    4 |    1000 B |        0.48 |
| &#39;Serialize Policy (Large: 10 rules, target + conditions)&#39; |    693.7 μs |    NA |  1.09 |    2 |   37008 B |       17.93 |
| &#39;Deserialize PolicySet (Small)&#39;                           | 35,450.5 μs |    NA | 55.77 |    8 |    2872 B |        1.39 |
| &#39;Deserialize PolicySet (Medium)&#39;                          | 40,135.0 μs |    NA | 63.14 |    9 |   35952 B |       17.42 |
| &#39;Deserialize PolicySet (Large)&#39;                           | 55,656.7 μs |    NA | 87.56 |   11 |  711008 B |      344.48 |
| &#39;Deserialize Policy (Small)&#39;                              | 33,838.2 μs |    NA | 53.23 |    6 |    1424 B |        0.69 |
| &#39;Deserialize Policy (Large)&#39;                              | 50,734.3 μs |    NA | 79.81 |   10 |   65816 B |       31.89 |
| &#39;Round-Trip PolicySet (Small)&#39;                            | 35,443.7 μs |    NA | 55.76 |    7 |    4936 B |        2.39 |
| &#39;Round-Trip PolicySet (Large)&#39;                            | 57,025.4 μs |    NA | 89.71 |   12 | 1113016 B |      539.25 |
