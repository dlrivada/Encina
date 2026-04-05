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
| &#39;Serialize PolicySet (Small: 1 policy, 2 rules)&#39;          |    658.4 μs |    NA |  1.00 |    1 |    2064 B |        1.00 |
| &#39;Serialize PolicySet (Medium: 3 policies, 15 rules)&#39;      |    726.2 μs |    NA |  1.10 |    3 |   22464 B |       10.88 |
| &#39;Serialize PolicySet (Large: 10 policies, 100 rules)&#39;     |  1,756.2 μs |    NA |  2.67 |    5 |  402480 B |      195.00 |
| &#39;Serialize Policy (Small: 2 rules, no target)&#39;            |    677.9 μs |    NA |  1.03 |    2 |    1000 B |        0.48 |
| &#39;Serialize Policy (Large: 10 rules, target + conditions)&#39; |    766.9 μs |    NA |  1.16 |    4 |   37008 B |       17.93 |
| &#39;Deserialize PolicySet (Small)&#39;                           | 36,441.5 μs |    NA | 55.35 |    7 |    2872 B |        1.39 |
| &#39;Deserialize PolicySet (Medium)&#39;                          | 41,383.4 μs |    NA | 62.86 |    9 |   35952 B |       17.42 |
| &#39;Deserialize PolicySet (Large)&#39;                           | 57,405.5 μs |    NA | 87.19 |   11 |  760208 B |      368.32 |
| &#39;Deserialize Policy (Small)&#39;                              | 36,179.9 μs |    NA | 54.95 |    6 |    1424 B |        0.69 |
| &#39;Deserialize Policy (Large)&#39;                              | 52,405.9 μs |    NA | 79.60 |   10 |   65816 B |       31.89 |
| &#39;Round-Trip PolicySet (Small)&#39;                            | 37,026.7 μs |    NA | 56.24 |    8 |    4936 B |        2.39 |
| &#39;Round-Trip PolicySet (Large)&#39;                            | 59,806.6 μs |    NA | 90.84 |   12 | 1113016 B |      539.25 |
