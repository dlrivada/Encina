```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.33GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean         | Error        | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-------------:|-------------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |   9,035.7 ns |  7,202.59 ns | 394.80 ns |  1.00 |    0.05 |    5 |  0.2594 | 0.1068 |    4690 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |   8,672.1 ns |  2,536.29 ns | 139.02 ns |  0.96 |    0.04 |    5 |  0.2594 | 0.1221 |    4756 B |        1.01 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   2,321.1 ns |  1,634.14 ns |  89.57 ns |  0.26 |    0.01 |    3 |  0.0305 |      - |     512 B |        0.11 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   1,252.1 ns |     94.42 ns |   5.18 ns |  0.14 |    0.01 |    2 |  0.0229 |      - |     392 B |        0.08 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   3,985.1 ns |    518.07 ns |  28.40 ns |  0.44 |    0.02 |    4 |  0.0610 |      - |    1048 B |        0.22 |
| &#39;Anonymize: data masking (2 fields)&#39;      |     820.4 ns |     58.85 ns |   3.23 ns |  0.09 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.20 |
| &#39;Risk assessment: 100-record dataset&#39;     | 192,548.2 ns | 11,918.29 ns | 653.28 ns | 21.34 |    0.82 |    6 | 17.3340 | 0.4883 |  291296 B |       62.11 |
