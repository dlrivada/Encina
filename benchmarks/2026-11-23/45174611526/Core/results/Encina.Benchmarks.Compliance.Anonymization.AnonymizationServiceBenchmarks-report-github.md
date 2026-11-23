```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.20GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean         | Error       | StdDev      | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-------------:|------------:|------------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |   8,413.4 ns |  3,828.4 ns |   209.85 ns |  1.00 |    0.03 |    5 |  0.1984 | 0.0610 |    3496 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |   8,074.5 ns |  1,233.2 ns |    67.60 ns |  0.96 |    0.02 |    5 |  0.1984 | 0.0610 |    3568 B |        1.02 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   2,229.5 ns |    144.1 ns |     7.90 ns |  0.27 |    0.01 |    3 |  0.0305 |      - |     512 B |        0.15 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   1,274.7 ns |    119.3 ns |     6.54 ns |  0.15 |    0.00 |    2 |  0.0229 |      - |     392 B |        0.11 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   3,983.3 ns |  1,419.0 ns |    77.78 ns |  0.47 |    0.01 |    4 |  0.0610 |      - |    1048 B |        0.30 |
| &#39;Anonymize: data masking (2 fields)&#39;      |     807.8 ns |    720.8 ns |    39.51 ns |  0.10 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.27 |
| &#39;Risk assessment: 100-record dataset&#39;     | 196,172.7 ns | 98,134.6 ns | 5,379.09 ns | 23.33 |    0.74 |    6 | 17.3340 | 0.4883 |  291296 B |       83.32 |
