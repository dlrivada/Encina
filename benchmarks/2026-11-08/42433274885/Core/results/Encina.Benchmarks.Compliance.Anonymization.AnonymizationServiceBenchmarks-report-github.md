```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 3.95GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                    | Mean         | Error       | StdDev      | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-------------:|------------:|------------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |   9,383.3 ns |   370.99 ns |   520.08 ns |  1.00 |    0.08 |    5 |  0.1984 | 0.0610 |    3504 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |   9,624.1 ns |   586.18 ns |   840.68 ns |  1.03 |    0.11 |    5 |  0.2136 | 0.0763 |    3584 B |        1.02 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   2,349.7 ns |    67.33 ns |   100.77 ns |  0.25 |    0.02 |    3 |  0.0305 |      - |     512 B |        0.15 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   1,327.5 ns |    39.47 ns |    59.08 ns |  0.14 |    0.01 |    2 |  0.0229 |      - |     392 B |        0.11 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   4,032.1 ns |   120.04 ns |   175.96 ns |  0.43 |    0.03 |    4 |  0.0610 |      - |    1048 B |        0.30 |
| &#39;Anonymize: data masking (2 fields)&#39;      |     822.9 ns |    21.67 ns |    32.43 ns |  0.09 |    0.01 |    1 |  0.0553 |      - |     936 B |        0.27 |
| &#39;Risk assessment: 100-record dataset&#39;     | 202,666.6 ns | 4,342.38 ns | 6,499.48 ns | 21.66 |    1.39 |    6 | 17.3340 | 0.4883 |  291296 B |       83.13 |
