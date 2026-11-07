```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.15GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean         | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-------------:|------------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |   8,802.7 ns |  1,201.5 ns |  65.86 ns |  1.00 |    0.01 |    5 |  0.2289 | 0.0916 |    4093 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |   8,689.7 ns |  8,823.2 ns | 483.63 ns |  0.99 |    0.05 |    5 |  0.2289 | 0.0916 |    4168 B |        1.02 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   2,347.0 ns |    694.2 ns |  38.05 ns |  0.27 |    0.00 |    3 |  0.0305 |      - |     512 B |        0.13 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   1,213.0 ns |    129.7 ns |   7.11 ns |  0.14 |    0.00 |    2 |  0.0229 |      - |     392 B |        0.10 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   3,981.6 ns |  1,209.7 ns |  66.31 ns |  0.45 |    0.01 |    4 |  0.0610 |      - |    1048 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |     815.2 ns |    361.9 ns |  19.84 ns |  0.09 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.23 |
| &#39;Risk assessment: 100-record dataset&#39;     | 192,959.9 ns | 14,778.9 ns | 810.08 ns | 21.92 |    0.16 |    6 | 17.3340 | 0.4883 |  291296 B |       71.17 |
