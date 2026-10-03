```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean         | Error       | StdDev      | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-------------:|------------:|------------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |   8,612.2 ns |  4,002.0 ns |   219.36 ns |  1.00 |    0.03 |    5 |  0.2289 | 0.0916 |    4222 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |   8,662.8 ns |  5,261.0 ns |   288.37 ns |  1.01 |    0.04 |    5 |  0.2441 | 0.0916 |    4290 B |        1.02 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   2,647.1 ns |    354.9 ns |    19.45 ns |  0.31 |    0.01 |    3 |  0.0343 |      - |     632 B |        0.15 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   1,379.2 ns |    110.2 ns |     6.04 ns |  0.16 |    0.00 |    2 |  0.0229 |      - |     384 B |        0.09 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   4,613.7 ns |  1,200.6 ns |    65.81 ns |  0.54 |    0.01 |    4 |  0.0610 |      - |    1088 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |     801.0 ns |    320.8 ns |    17.58 ns |  0.09 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.22 |
| &#39;Risk assessment: 100-record dataset&#39;     | 192,752.7 ns | 19,231.3 ns | 1,054.13 ns | 22.39 |    0.50 |    6 | 17.3340 | 0.4883 |  291296 B |       68.99 |
