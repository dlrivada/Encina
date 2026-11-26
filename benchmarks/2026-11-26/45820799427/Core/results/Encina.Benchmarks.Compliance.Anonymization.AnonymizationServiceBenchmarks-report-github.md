```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.25GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean         | Error       | StdDev      | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-------------:|------------:|------------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |   8,487.0 ns |  3,295.0 ns |   180.61 ns |  1.00 |    0.03 |    5 |  0.2594 | 0.1221 |    4699 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |   8,520.1 ns |  1,289.8 ns |    70.70 ns |  1.00 |    0.02 |    5 |  0.1984 | 0.0610 |    3568 B |        0.76 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   2,228.3 ns |    208.0 ns |    11.40 ns |  0.26 |    0.01 |    3 |  0.0305 |      - |     512 B |        0.11 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   1,247.5 ns |    724.3 ns |    39.70 ns |  0.15 |    0.00 |    2 |  0.0229 |      - |     392 B |        0.08 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   3,920.9 ns |  1,655.3 ns |    90.73 ns |  0.46 |    0.01 |    4 |  0.0610 |      - |    1048 B |        0.22 |
| &#39;Anonymize: data masking (2 fields)&#39;      |     819.5 ns |    464.5 ns |    25.46 ns |  0.10 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.20 |
| &#39;Risk assessment: 100-record dataset&#39;     | 196,122.1 ns | 28,218.8 ns | 1,546.77 ns | 23.12 |    0.46 |    6 | 17.3340 | 0.4883 |  291296 B |       61.99 |
